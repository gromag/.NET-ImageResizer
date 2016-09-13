using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace grom.lib.graphics
{
	public class ImageResizer
	{
		/// <summary>
		/// A quick lookup for getting image encoders
		/// </summary>
		private static Dictionary<string, ImageCodecInfo> encoders = null;

		/// <summary>
		/// A quick lookup for getting image encoders
		/// </summary>
		public static Dictionary<string, ImageCodecInfo> Encoders
		{
			//get accessor that creates the dictionary on demand
			get
			{
				//if the quick lookup isn't initialised, initialise it
				if (encoders == null)
				{
					encoders = new Dictionary<string, ImageCodecInfo>();
				}

				//if there are no codecs, try loading them
				if (encoders.Count == 0)
				{
					//get all the codecs
					foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
					{
						//add each codec to the quick lookup
						encoders.Add(codec.MimeType.ToLower(), codec);
					}
				}

				//return the lookup
				return encoders;
			}
		}

		/// <summary>
		/// *************************************************************
		/// Resizes or crops an images to the requested width and height.
		/// *************************************************************
		/// If any dimension is not passed, the function will calculate the missed dimensions
		/// If both dimensions are not passed, the function will return a *copy* of the original image
		/// If any of the requested dimensions exceeds the original one's, this function will return null
		/// If dimensions' ratio does not match the original ratio, clipping will occur.
		/// </summary>
		/// <param name="sourceImage"></param>
		/// <param name="targetWidth"></param>
		/// <param name="targetHeight"></param>
		/// <returns>A bitmap, you will **need** to dispose of such image</returns>
		public static byte[] Resize(Image sourceImage, int? targetWidth, int? targetHeight, int quality)
		{
			//w, h source width and height
			int w = sourceImage.Size.Width;
			int h = sourceImage.Size.Height;
			//wt, ht requested width and height
			int wt = 1;
			int ht = 1;

			//The new image would exceed the max boundary of the source image
			if (targetWidth > w || targetHeight > h) return null;

			if (targetWidth == null && targetHeight == null)
			{
				wt = w;
				ht = h;
			}

			var sourceRatio = (double)w / (double)h;

			//if no target width expressed then
			//if w = sourceRatio * h
			//then
			//wt = sourceRatio * ht
			wt = (int)(targetWidth ?? sourceRatio * ht);

			//if no target height expressed then
			//if h = w/sourceRatio
			//then
			//ht = wt/sourceRatio
			ht = (int)(targetHeight ?? wt / sourceRatio);

			var targetRatio = (double)wt / (double)ht;

			#region ***Clipping explaination in visual terms
			//Clip applied to original image before scaling
			// If proportions are as follow:
			//
			//			target 2:1				 source 1:1
			//		 ___________			 _______________
			//		|			|			|				|
			//		|___________|			|				|
			//								|				|
			//								|				|
			//								|_______________|
			// Then we will clip as follows:
			//
			//				  clip to source
			//				 _______________
			//				|_ _ _ _ _ _ _ _|
			//				|				|
			//				|				|
			//				|_ _ _ _ _ _ _ _|
			//				|_______________|

			// or vertical clip instead if proportions are as follow:
			//
			//			target 1:2				 source 1:1
			//		 ___					 _______________
			//		|	|					|				|
			//		|	|					|				|
			//		|___|					|				|
			//								|				|
			//								|_______________|
			// Then we will clip as follows:
			//
			//				  clip to source
			//				 _______________
			//				|	 !	   !	|
			//				|	 !	   !	|
			//				|	 !	   !	|
			//				|	 !	   !	|
			//				|____!_____!____|
			#endregion
			Rectangle clip;

			if (targetRatio >= sourceRatio)
			{
				//The image requested is more elungated than the original  one 
				//therefore we clip the height
				//targetRatio = wt/ht
				//ht = wt/targetRatio
				//hClip = w/targetRatio
				var hClip = (int)Math.Ceiling((double)w / (double)targetRatio);

				//Rectangle pars are: x, y, width, height
				clip = new Rectangle(0, (h - hClip) / 2, w, hClip);
			}
			else
			{
				//The image requested is more stretched in height than the original  one 
				//therefore we clip the width
				//targetRatio = wt/ht
				//wt = targetRatio * ht
				//hClip = targetRatio * h
				var wClip = (int)Math.Ceiling((double)h * (double)targetRatio);

				//Rectangle pars are: x, y, width, height
				clip = new Rectangle((w - wClip) / 2, 0, wClip, h);
			}


			var targetImage = new Bitmap(wt, ht);

			using (var g = Graphics.FromImage(targetImage))
			{
				g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;


				var targetRectangle = new Rectangle(0, 0, wt, ht);

				g.DrawImage(sourceImage, targetRectangle, clip, GraphicsUnit.Pixel);
			}

			var bytes = ImageToByteArray(targetImage, ImageFormat.Jpeg, quality);

			targetImage.Dispose();

			return bytes;
		}
		/// <summary>
		/// Given a System.Drawing.Image it will return the corresponding byte array given 
		/// a format.
		/// </summary>
		/// <param name="imageIn"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		public static byte[] ImageToByteArray(System.Drawing.Image imageIn, ImageFormat format, int quality)
		{
			//create an encoder parameter for the image quality
			EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
			//get the jpeg codec
			ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

			//create a collection of all parameters that we will pass to the encoder
			EncoderParameters encoderParams = new EncoderParameters(1);
			//set the quality parameter for the codec
			encoderParams.Param[0] = qualityParam;
			//save the image using the codec and the parameters

			var ms = new MemoryStream();
			imageIn.Save(ms, jpegCodec, encoderParams);
			return ms.ToArray();
		}
		/// <summary>
		/// Converts a byte array to System.Drawing.Image.
		/// IMPORTANT: You must dispose of the returned image.
		/// </summary>
		/// <param name="byteArrayIn"></param>
		/// <returns>Returns an image of type System.Drawing.Image, you will have to take care of disposing it</returns>
		public static Image ByteArrayToImage(byte[] byteArrayIn)
		{
			var ms = new MemoryStream(byteArrayIn);
			var returnImage = Image.FromStream(ms);
			return returnImage;
		}

		/// <summary> 
		/// Returns the image codec with the given mime type 
		/// </summary> 
		public static ImageCodecInfo GetEncoderInfo(string mimeType)
		{
			//do a case insensitive search for the mime type
			string lookupKey = mimeType.ToLower();

			//the codec to return, default to null
			ImageCodecInfo foundCodec = null;

			//if we have the encoder, get it to return
			if (Encoders.ContainsKey(lookupKey))
			{
				//pull the codec from the lookup
				foundCodec = Encoders[lookupKey];
			}

			return foundCodec;
		}
	}
}

