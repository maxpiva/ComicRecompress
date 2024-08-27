using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using System.Xml.Linq;
using CliWrap;
using CliWrap.Buffered;
using ComicRecompress.Chainner;
using ComicRecompress.Chainner.Models;
using ComicRecompress.Jobs;
using ComicRecompress.Models;
using Pastel;

namespace ComicRecompress.Services
{

    public class Chainner
    {
        private readonly BaseJob _job;
        private readonly ChainnerClient _client;
        public Chainner(BaseJob job)
        {
            _job = job;
            if (Program.Backend!=null)
                _client = new ChainnerClient($"http://localhost:{Program.Backend.Port}", Program.Backend.Settings);
        }

        private string LoadNodeId;
        private ChnFile? GenerateOverrides(string input, string output, string chn)
        {
            if (!File.Exists(chn))
                return null;
            ChnFile? chain = JsonSerializer.Deserialize<ChnFile>(File.ReadAllText(chn), SerializerGenerationContext.Options);
            if (chain == null)
                return null;
            ChnNode? load = chain.content.nodes.FirstOrDefault(a => a.data.schemaId.Contains("image:load_images"));
            ChnNode? save = chain.content.nodes.FirstOrDefault(a => a.data.schemaId.Contains("image:save"));
            if (load == null || save == null)
                return null;
            load.data.inputData[0] = input;
            LoadNodeId = load.id;
            save.data.inputData[1] = output;
            return chain;

        }

        private string? Exists(string filename, string basePath, string? contpath=null)
        {
            string localName = Path.GetFileName(filename);
            if (contpath!=null)
                localName = Path.Combine(contpath, localName);
            string localname= Path.Combine(basePath, localName);
            if (File.Exists(localname))
                return localname;
            return null;
        }

        private string? VerifyLocalName(string filename, string? type)
        {
            string? final = Exists(filename, Environment.CurrentDirectory);
            if (final!=null)
                return final;
            final = Exists(filename, Environment.CurrentDirectory, "chainsandmodels");
            if (final != null)
                return final;
            if (type != null)
            {
                final = Exists(filename, Environment.CurrentDirectory, $"chainsandmodels\\{type}");
                if (final != null)
                    return final;
            }
            final = Exists(filename, System.AppContext.BaseDirectory);
            if (final != null)
                return final;
            final = Exists(filename, System.AppContext.BaseDirectory, "chainsandmodels");
            if (type != null)
            {
                if (final != null)
                    return final;
                final = Exists(filename, System.AppContext.BaseDirectory, $"chainsandmodels\\{type}");
            }
            return final;
        }

        private bool VerifyModels(ChnFile file)
        {

            //onnx 
            List<ChnNode> models=file.content.nodes.Where(a => a.data.schemaId.Contains("onnx:load_model")).ToList();
            foreach (ChnNode n in models)
            {

                string filename = n.data.inputData[0].ToString();
                string localName = VerifyLocalName(filename, "onnx");
                if (localName==null)
                {
                    _job.WriteError($"ERROR: Model file not found: {n.data.inputData[0]}");
                    return false;
                }
                n.data.inputData[0] = Path.GetFullPath(localName);
            }
            //pytorch
            models = file.content.nodes.Where(a => a.data.schemaId.Contains("pytorch:load_model")).ToList();
            foreach (ChnNode n in models)
            {

                string filename = n.data.inputData[0].ToString();
                string localName = VerifyLocalName(filename, "pytorch");
                if (localName == null)
                {
                    _job.WriteError($"ERROR: Model file not found: {n.data.inputData[0]}");
                    return false;
                }
                n.data.inputData[0] = Path.GetFullPath(localName);
            }
            //ncnn
            models = file.content.nodes.Where(a => a.data.schemaId.Contains("ncnn:load_model")).ToList();
            foreach (ChnNode n in models)
            {
                string filename1 = n.data.inputData[0].ToString();
                string filename2 = n.data.inputData[1].ToString();
                string localName = VerifyLocalName(filename1, "ncnn");
                if (localName == null)
                {
                    _job.WriteError($"ERROR: Parameter file not found: {n.data.inputData[0]}");
                    return false;
                }
                n.data.inputData[0] = Path.GetFullPath(localName);
                localName = VerifyLocalName(filename2, "ncnn");
                if (localName == null)
                {
                    _job.WriteError($"ERROR: Model file not found: {n.data.inputData[1]}");
                    return false;
                }
                n.data.inputData[1] = Path.GetFullPath(localName);
            }

            return true;
        }

        public bool VerifyChainnerFile(string chn)
        {
            string localName = VerifyLocalName(chn, null);
            if (localName==null)
            {
                _job.WriteError($"ERROR: Chainner file not found: {chn}");
                return false;
            }
            ChnFile file = GenerateOverrides("","", localName);
            if (file == null)
            {
                _job.WriteError($"ERROR:loading chaiNNer file {chn}: Unable to create input overrides");
                return false;
            }
            return VerifyModels(file);
        }


        public async Task<bool> ExecuteAsync(string input, string output, string chn)
        {
            try
            {
                string localName = VerifyLocalName(chn, null);
                if (localName == null)
                {
                    _job.WriteError($"ERROR: Chainner file not found: {chn}");
                    return false;
                }
                ChnFile file = GenerateOverrides(input, output, localName);
                if (file == null)
                {
                    _job.WriteError("ERROR:executing chaiNNer: Unable to create input overrides");
                    return false;
                }

                if (!VerifyModels(file))
                    return false;
                _client.OnMessage += _backend_OnMessage;
                bool res = await _client.RunChnAsync(file).ConfigureAwait(false);
                _client.OnMessage -= _backend_OnMessage;
                if (!res)
                {
                    _job.WriteError($"ERROR: Unable to execute chainner job.");
                    return false;
                }
                return true;


            }
            catch (Exception e)
            {
                _job.WriteError($"ERROR:executing chaiNNer: {e.Message}");
                return false;
            }

            return true;
        }

        private void _backend_OnMessage(object sender, BaseEvent message)
        {

            if (message is NodeProgress msg && msg.NodeId == LoadNodeId)
            {
                if (msg.Index < msg.Total)
                {
                    string eta = "N/A";
                    if (msg.Eta > 0.0m)
                    {
                        double et = decimal.ToDouble(msg.Eta);
                        eta = DateTime.Now.AddSeconds(et).ToShortTimeString();
                    }
                    _job.WriteLine($"Processing Image [{msg.Index + 1:000}/{msg.Total:000}] ETA: {eta}");
                }
            }
        }

        public class Override
        {
            public Dictionary<string, string> inputs { get; set; } = new Dictionary<string, string>();
        }
        public class Chn
        {
            public Content content { get; set; }
        }
        public class Content
        {
            public List<Node> nodes { get; set; }
        }
        public class Node
        {
            public Data data { get; set; }
        }
        public class Data
        {
            public string id { get; set; }
            public string schemaId { get; set; }
        }
    }

}
