# ComicRecompress
ComicRecompress marries two state of the art technologies: neural network image processing and JPEG-XL

Clean and/or recompress your comics/manga/manhwas. Removing artifacts, and reducing the overall space used.

## Supported Modes

### Chainner Mode

Type of images: Comics with JPEG Compression, Halo and other Artifacts.

Uses: chaiNNer + Jpeg-XL 

*  Using neural network models to remove image compression artifacts, or state of the AI scaling algorithms, for low resolution images. You can feed you own chaiNNer chain files.
*  Using JPEG-XL modular mode which extremely compress the artifact free result, into something that most of the time, its smaller that the original source file, and remains with visual free artifacts.

### Reconstruct Mode

Type of images: JPEG Images, recompress existing archive into smaller size, without modifying the original JPEG. 

Uses: JPEG-XL

* Recompress existing JPGs to JPEG-XL obtaining 5-30% reduction in size using JPEG-XL VarDCT mode. JPEG XL can losslessly recompress existing JPEG files and go back.

_This mode is great if you don't want to touch the original files, but you want extra storage compression. Reducing the overall size of the archive, without any visual modification. And you can always go back to original jpeg back._

### Modular Mode

Types of images: High Quality JPEG or PNG without artifacts.

Uses: JPEG-XL

* Recompress existing PNG LossLess or Almost Lossless Jpeg into JPEG-XL Modular model (Cannot go back to the original JPEG), reducing the size, without any visible loss.

_This mode is useful when the original files do not have artifacts, and you want to reduce archive size dramatically without visible loss._


## Requirements 

* Windows... Currently (Linux/Mac can be supported in the future, there is no limitation, only testing, and spawn execution of the tools,  will accept PRs)
* ChaiNNer - Download it from https://github.com/chaiNNer-org/chaiNNer/releases
* JPEG-XL - cjxl.exe (JPEG Compressor) from https://github.com/libjxl/libjxl/releases

## Almost Required:

Nvidia Video Card (chaiNNer works in CPU mode albeit slow, and NCNN models work in AMD cards). Please follow chaiNNer guides if you don't have an nvidia card. NCNN is your friend in that case.

## Windows Installation

1) Download JPEG-XL, make sure cjxl.exe is in the same directory as ComicRecompress. (cjxl.exe is in JPEG-XL bin directory)
2) Install Chainer.
3) Run Chainner, and install all chainner requirements including integrated Python, PyTorch, Onnx and NCNN. All requirements need to be installed before running this.
4) Adjust Setting according, read below.
4) Quit chainner.

## Important

ComicCompress uses chaiNNer backend only, so you cannot run comicCompress and chaiNNer at the same time. ComicCompress will spawn a chaiNNer backend when run, and kill it when it quits.

## Tweaking

The Included chainner files, can be tweaked in chaiNNer and new chainner files can be created, or it can be used as an example to create your own.
There is three variation of the same chain, one tailored for pytorch, other for onnx and the last one for ncnn models.

IE: **default-onnx.chn**
Currently the chain  does the following:

1) Adds a 16 pixel border to the images, and mirror the edges. This is mostly required for WebComics (Manhwa), so when they're viewed vertically there is no visible lines between images.
2) Uses foolhardy Remacri neural model to rescale 4X the original image, removing artifacts, in the process.
3) Scale back 50%
4) Uses AnimeUndeint model to improve comic draw lines.
5) Scale back 50%
6) remove 16 pixel border.
7) Save as 48 bit png.
![image](https://github.com/user-attachments/assets/dd995e58-0dcb-4f91-bbae-e14ff1f99923)

_Since the program can take any chainner save file as a parameter you're open to experiment. If you create something usefull, please do not hesitate to do a pull request._

The current chainner file was an experimentation by me, looking for a good compromise in artifact removal without texture crushing. But new models are created every day, and other can come with better chainner files.

I found onnx models seems to be faster that pytorch and NCNN models. (In my machine).

Most of the time you can convert the pytorch model to ONNX or to NCNN using chainner. Follow chaiNNer guides.

If you plan to use your computer, at the same time, its usefull to limit the VRAM use by Chainner, run Chainner, go to Settings and adjust. Also enable any cache. As rule of thumb, try to reduce the tile size of the upscalers to the minimun, so the VRAM usage will be less. 

Additional models to expermiment can be downloaded from here: https://openmodeldb.info/ so, you can experiment free.

## Usage

Supports rar and zip archives containing images, .rar, .zip, .7z, .cbr, .cbz. .cb7
Destination directory will recreate the directory structure of the input directory, comiocompress will scan for archives recursively. any non images files in archives will be copied to the destination archives.
Output format will be cbz files without compression.

```console
C:\Sources\Repos\ComicRecompress\bin\Release\net8.0\publish\win-x64>ComicRecompress.exe --help
ComicRecompress 1.0.0
Copyright (C) 2024 ComicRecompress

  -i, --input                  Required. Input File or Directory

  -o, --output                 Required. Output Directory

  -c, --chain                  (Default: default-onnx.chn) ChaiNNer chain file

  -m, --mode                   (Default: chaiNNer) Mode of Execution [chaiNNer|Reconstruct|Modular]

  -w, --webcomic               (Default: 0) WebComic/Manhwa, Join same width images, into a bigger images before processing, specifying the maximum height of the final
                               images [0 - Disabled]

  -q, --quality                (Default: 95) JPEG XL Quality for Chainner/Modular Mode

  -p, --port                   (Default: 8000) Chainner Backend Port

  -t, --tasks                  (Default: 2) Number of Comics that can be processed at the same time doing different tasks

  -j, --jpegXLThreads          (Default: 4) Number of JPEG XL Threads running at the same time

  -k, --tasksRestartBackend    (Default: 5) Number of tasks before restarting the backend [0 = Never]

  --help                       Display this help screen.

  --version                    Display version information.
```
### Tidbits
*  tasksRestartBackend, is used to restart the backend after processing #N archives, VRAM gets fragmented, and get leaks, so restarting the backend ensures, good VRAM behavior.






