#define TRACE
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace NTE_Fishing_Bot;

public class ScreenStateLogger
{
	private bool _run;

	private IAppSettings settings;

	public EventHandler<byte[]> ScreenRefreshed;

	public EventHandler<string> CaptureError;

	public int Size { get; private set; }

	public ScreenStateLogger(IAppSettings _settings)
	{
		settings = _settings;
	}

	public void Start()
	{
		_run = true;
		Task.Factory.StartNew(delegate
		{
			Factory1 factory = null;
			Adapter1 adapter = null;
			SharpDX.Direct3D11.Device device = null;
			Output output = null;
			Output1 output2 = null;
			Texture2D screenTexture = null;
			try
			{
				factory = new Factory1();
				adapter = factory.GetAdapter1(settings.DefaultAdapter);
				_ = factory.Adapters;
				device = new SharpDX.Direct3D11.Device(adapter);
				output = adapter.GetOutput(settings.DefaultDevice);
				output2 = output.QueryInterface<Output1>();
				int width = output.Description.DesktopBounds.Right - output.Description.DesktopBounds.Left;
				int height = output.Description.DesktopBounds.Bottom - output.Description.DesktopBounds.Top;
				Texture2DDescription textureDesc = new Texture2DDescription
				{
					CpuAccessFlags = CpuAccessFlags.Read,
					BindFlags = BindFlags.None,
					Format = Format.B8G8R8A8_UNorm,
					Width = width,
					Height = height,
					OptionFlags = ResourceOptionFlags.None,
					MipLevels = 1,
					ArraySize = 1,
					SampleDescription = { Count = 1, Quality = 0 },
					Usage = ResourceUsage.Staging
				};
				screenTexture = new Texture2D(device, textureDesc);

				using OutputDuplication outputDuplication = output2.DuplicateOutput(device);
				int consecutiveErrors = 0;
				while (_run)
				{
					try
					{
						outputDuplication.TryAcquireNextFrame(5, out var _, out var desktopResourceOut);
						if (desktopResourceOut != null)
						{
							using (Texture2D source = desktopResourceOut.QueryInterface<Texture2D>())
							{
								device.ImmediateContext.CopyResource(source, screenTexture);
							}
							DataBox dataBox = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
							using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
							{
								Rectangle rect = new Rectangle(0, 0, width, height);
								BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
								IntPtr intPtr = dataBox.DataPointer;
								IntPtr intPtr2 = bitmapData.Scan0;
								for (int i = 0; i < height; i++)
								{
									Utilities.CopyMemory(intPtr2, intPtr, width * 4);
									intPtr = IntPtr.Add(intPtr, dataBox.RowPitch);
									intPtr2 = IntPtr.Add(intPtr2, bitmapData.Stride);
								}
								bitmap.UnlockBits(bitmapData);
								device.ImmediateContext.UnmapSubresource(screenTexture, 0);
								using MemoryStream memoryStream = new MemoryStream();
								bitmap.Save(memoryStream, ImageFormat.Bmp);
								ScreenRefreshed?.Invoke(this, memoryStream.ToArray());
							}
							desktopResourceOut.Dispose();
							outputDuplication.ReleaseFrame();
						}
						consecutiveErrors = 0;
					}
					catch (SharpDXException ex)
					{
						if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
							continue;

						consecutiveErrors++;
						Trace.TraceError(ex.Message);
						if (consecutiveErrors >= 10)
						{
							CaptureError?.Invoke(this, ex.ResultCode.Code + ": " + ex.Message);
							_run = false;
						}
						System.Threading.Thread.Sleep(100);
					}
					catch (Exception ex)
					{
						consecutiveErrors++;
						Trace.TraceError(ex.Message);
						if (consecutiveErrors >= 10)
						{
							CaptureError?.Invoke(this, ex.Message);
							_run = false;
						}
						System.Threading.Thread.Sleep(100);
					}
				}
			}
			finally
			{
				screenTexture?.Dispose();
				output2?.Dispose();
				output?.Dispose();
				device?.Dispose();
				adapter?.Dispose();
				factory?.Dispose();
			}
		});
	}

	public void Stop()
	{
		_run = false;
	}
}
