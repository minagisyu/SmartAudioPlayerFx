using FFmpeg.AutoGen;
using OpenTK;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Console;
using static FFmpeg.AutoGen.ffmpeg;
using System.IO;
using System.Runtime.InteropServices;

namespace player_application
{

	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private unsafe void button1_Click(object sender, EventArgs e)
		{
			Task.Run(() =>
			{
				av_register_all();

				AVFormatContext* pFormatCtx = null;

				// open media file
				string file = @"V:\__Video(old)\1995-2010\_etc\AT-X ロゴ (AT-X 640x480_x264 [2009Q3]).mp4";
				file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x720p Hi10P).mp4";
				if (avformat_open_input(&pFormatCtx, file, null, null) != 0)
				{
					WriteLine("Error opening the file");
					return;
				}

				// retrive stream infomation 
				if (avformat_find_stream_info(pFormatCtx, null) < 0)
				{
					avformat_close_input(&pFormatCtx);
					WriteLine("Error finding the stream info");
					return;
				}

				// Dump information about file onto standard error
				av_dump_format(pFormatCtx, 0, file, 0);

				// Find the first video stream
				AVCodec* pCodec = null;
				int streamIndex = ffmpeg.av_find_best_stream(
					pFormatCtx,
					//	AVMediaType.AVMEDIA_TYPE_AUDIO,
					AVMediaType.AVMEDIA_TYPE_VIDEO,
					-1, -1, &pCodec, 0);
				if (streamIndex < 0)
				{
					avformat_close_input(&pFormatCtx);
					WriteLine("Didn't find a video stream");
					return;
				}

				// Get a pointer to the codec context for the video stream
				AVStream* videoStream = pFormatCtx->streams[streamIndex];
				AVCodecContext* pCodecCtx = videoStream->codec;
				pCodecCtx->codec = pCodec;

				// Open codec
				if (avcodec_open2(pCodecCtx, pCodecCtx->codec, null) < 0)
				{
					avcodec_close(pCodecCtx);
					avformat_close_input(&pFormatCtx);
					WriteLine("Couldn't open the context with the decoder");
					return;
				}

				WriteLine($"This stream has {pCodecCtx->channels} channels and a sample rate of {pCodecCtx->sample_rate} Hz");
				WriteLine($"The data is in the format {av_get_sample_fmt_name(pCodecCtx->sample_fmt)}");

				// Allocate video frame
				AVFrame* pFrame = av_frame_alloc();

				// Allocate an AVFrame structure
				AVFrame* pFrameRGB = av_frame_alloc();
				if (pFrameRGB == null)
					return;

				// Determine required buffer size and allocate buffer
				int numBytes = avpicture_get_size(AVPixelFormat.AV_PIX_FMT_RGB24, pCodecCtx->width,
								pCodecCtx->height);
				byte* buffer = (byte*)av_malloc((ulong)numBytes * sizeof(byte));

				// Assign appropriate parts of buffer to image planes in pFrameRGB
				// Note that pFrameRGB is an AVFrame, but AVFrame is a superset
				// of AVPicture
				avpicture_fill((AVPicture*)pFrameRGB, (sbyte*)buffer, AVPixelFormat.AV_PIX_FMT_RGB24,
					   pCodecCtx->width, pCodecCtx->height);

				// initialize SWS context for software scaling
				SwsContext* sws_ctx = sws_getContext(pCodecCtx->width,
							 pCodecCtx->height,
							 pCodecCtx->pix_fmt,
							 pCodecCtx->width,
							 pCodecCtx->height,
							 AVPixelFormat.AV_PIX_FMT_RGB24,
							 SWS_BILINEAR,
							 null,
							 null,
							 null
							 );

				// Read frames and save first five frames to disk
				int i = 0;
				int frameFinished = 0;
				AVPacket packet;
				double pts;
				while (av_read_frame(pFormatCtx, &packet) >= 0)
				{
					// Is this a packet from the video stream?
					if (packet.stream_index == streamIndex)
					{
						// Decode video frame
						avcodec_decode_video2(pCodecCtx, pFrame, &frameFinished, &packet);

						// Did we get a video frame?
						if (frameFinished != 0)
						{
							pts = packet.pts / 1000.0 / 60.0;
							this.Invoke(new Action(() => { this.Text = pts.ToString(); }));
							// Convert the image from its native format to RGB
							sws_scale(sws_ctx, &pFrame->data0,
			  pFrame->linesize, 0, pCodecCtx->height,
			  &pFrameRGB->data0, pFrameRGB->linesize);

							// Save the frame to disk
							SaveFrame(pFrameRGB, pCodecCtx->width, pCodecCtx->height, i);
						}
					}

					// Free the packet that was allocated by av_read_frame
					av_free_packet(&packet);
				}

				// Free the RGB image
				av_free(buffer);
				av_frame_free(&pFrameRGB);

				// Free the YUV frame
				av_frame_free(&pFrame);

				// Close the codecs
				avcodec_close(pCodecCtx);

				// Close the video file
				avformat_close_input(&pFormatCtx);
			});
		}

		unsafe void SaveFrame(AVFrame* pFrame, int width, int height, int iFrame)
		{
			if (this.IsDisposed) return;
			using (var g = this.CreateGraphics())
			using (var im = new Bitmap(width, height, pFrame->linesize[0],
				System.Drawing.Imaging.PixelFormat.Format24bppRgb, (IntPtr)pFrame->data0))
			{
				g.DrawImage(im, new RectangleF(0, 0, width, height));
			}
		}
	}

}
