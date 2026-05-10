using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace NTE_Fishing_Bot;

public static class BitmapExtension
{
	public unsafe static Bitmap Crop(this Bitmap bitmap, int left, int top, int width, int height)
	{
		Bitmap cropped = new Bitmap(width, height);
		BitmapData originalData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
		BitmapData croppedData = cropped.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
		int* srcPixel = (int*)(void*)originalData.Scan0 + (left + originalData.Width * top);
		int nextLine = originalData.Width - width;
		int y = 0;
		int i = 0;
		while (y < height)
		{
			int x = 0;
			while (x < width)
			{
				((int*)(void*)croppedData.Scan0)[i] = *srcPixel;
				x++;
				i++;
				srcPixel++;
			}
			y++;
			srcPixel += nextLine;
		}
		bitmap.UnlockBits(originalData);
		cropped.UnlockBits(croppedData);
		return cropped;
	}

	public unsafe static Bitmap CropSmall(this Bitmap bitmap, int left, int top, int width, int height)
	{
		Bitmap cropped = new Bitmap(width, height);
		BitmapData originalData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
		BitmapData croppedData = cropped.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
		Span<int> srcPixels = new Span<int>((void*)originalData.Scan0, originalData.Width * originalData.Height);
		int nextLine = originalData.Width - width;
		int y = 0;
		int i = 0;
		int s = left + originalData.Width * top;
		while (y < height)
		{
			int x = 0;
			while (x < width)
			{
				((int*)(void*)croppedData.Scan0)[i] = srcPixels[s];
				x++;
				i++;
				s++;
			}
			y++;
			s += nextLine;
		}
		bitmap.UnlockBits(originalData);
		cropped.UnlockBits(croppedData);
		return cropped;
	}
}
