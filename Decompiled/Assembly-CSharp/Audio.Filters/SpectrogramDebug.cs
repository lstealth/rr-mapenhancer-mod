using UnityEngine;

namespace Audio.Filters;

public class SpectrogramDebug : MonoBehaviour
{
	public int width = 512;

	public int height = 256;

	public FFTWindow fftWindow = FFTWindow.BlackmanHarris;

	private Texture2D texture;

	private Color[] pixels;

	private float[] spectrumData;

	private void Start()
	{
		texture = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
		pixels = new Color[width * height];
		spectrumData = new float[width];
	}

	private void Update()
	{
		AudioListener.GetSpectrumData(spectrumData, 0, fftWindow);
		for (int num = height - 1; num > 0; num--)
		{
			for (int i = 0; i < width; i++)
			{
				pixels[i + num * width] = pixels[i + (num - 1) * width];
			}
		}
		for (int j = 0; j < width; j++)
		{
			float num2 = spectrumData[j] * (float)height;
			for (int k = 0; k < height; k++)
			{
				if ((float)k < num2)
				{
					pixels[j + k * width] = Color.Lerp(Color.black, Color.white, num2 / (float)height);
				}
				else
				{
					pixels[j + k * width] = Color.black;
				}
			}
		}
		texture.SetPixels(pixels);
		texture.Apply();
	}

	private void OnGUI()
	{
		GUI.DrawTexture(new Rect(10f, 10f, width, height), texture);
	}
}
