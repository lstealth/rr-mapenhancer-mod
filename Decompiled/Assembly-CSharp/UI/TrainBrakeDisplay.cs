using System.Collections.Generic;
using Model;
using Model.Physics;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class TrainBrakeDisplay : MonoBehaviour
{
	[Header("Colors")]
	[SerializeField]
	private Gradient brakeCylinderGradient;

	[SerializeField]
	private Color handbrakeAppliedColor;

	[SerializeField]
	private Color derailedColor;

	[SerializeField]
	private Color airIssueColor;

	[Header("Sprites")]
	[SerializeField]
	private Sprite carTile;

	[SerializeField]
	private Sprite locomotiveTile;

	[SerializeField]
	private Sprite airIssueTile;

	private RectTransform _rectTransform;

	private readonly List<Image> _carImages = new List<Image>();

	private readonly List<Image> _airImages = new List<Image>();

	private int _lastNumCars = -1;

	private float _spacing = 1f;

	private float _imageWidth = 8f;

	private const float MaxSpacing = 1f;

	private const float MaxImageWidth = 8f;

	private const float ImageHeight = 12f;

	private TrainController _trainController => TrainController.Shared;

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
	}

	private void Update()
	{
		Car selectedCar = _trainController.SelectedCar;
		if (selectedCar == null || selectedCar.set == null)
		{
			return;
		}
		int numberOfCars = selectedCar.set.NumberOfCars;
		if (numberOfCars != _lastNumCars)
		{
			RemoveAllImages();
			int num = Mathf.Clamp(numberOfCars, 0, 100);
			float num2 = (float)num * 8f + (float)(num - 1) * 1f;
			if (num2 > _rectTransform.rect.width)
			{
				float num3 = _rectTransform.rect.width / num2;
				_imageWidth = 8f * num3;
				_spacing = 1f * num3;
			}
			else
			{
				_imageWidth = 8f;
				_spacing = 1f;
			}
			_lastNumCars = numberOfCars;
		}
		int num4 = 0;
		float num5 = _imageWidth / 2f;
		float num6 = 0f;
		float y = 12f - 2f * _spacing;
		Car.LogicalEnd logicalEnd = (((selectedCar.set.IndexOfCar(selectedCar) ?? 0) >= numberOfCars / 2) ? Car.LogicalEnd.B : Car.LogicalEnd.A);
		Car.LogicalEnd end = ((logicalEnd == Car.LogicalEnd.A) ? Car.LogicalEnd.B : Car.LogicalEnd.A);
		bool stop = false;
		int carIndex = selectedCar.set.StartIndexForConnected(selectedCar, logicalEnd, IntegrationSet.EnumerationCondition.Coupled);
		Car car;
		while (!stop && (car = selectedCar.set.NextCarConnected(ref carIndex, logicalEnd, IntegrationSet.EnumerationCondition.Coupled, out stop)) != null && !(num5 > _rectTransform.rect.width))
		{
			Image carImage = GetCarImage(num4, num5, 0f, car.IsLocomotive);
			num5 += _imageWidth + _spacing;
			carImage.color = ColorForCar(car);
			Image airImage = GetAirImage(num4, num6, y);
			num6 += _imageWidth + _spacing;
			Color color;
			if (!car[logicalEnd].IsCoupled)
			{
				color = ColorForOuterAnglecock(car[logicalEnd].AnglecockSetting);
			}
			else
			{
				Car car2 = car.CoupledTo(logicalEnd);
				bool num7 = car2 != null && car2[end].AnglecockSetting > 0.9f;
				bool flag = car[logicalEnd].AnglecockSetting > 0.9f;
				color = ((num7 && flag && car[logicalEnd].IsAirConnected) ? Color.clear : Color.white);
			}
			airImage.color = color;
			if (!car[end].IsCoupled)
			{
				GetAirImage(num4 + 1, num6, y).color = ColorForOuterAnglecock(car[end].AnglecockSetting);
			}
			num4++;
		}
		for (int i = num4; i < _carImages.Count; i++)
		{
			_carImages[i].gameObject.SetActive(value: false);
		}
		for (int j = num4 + 1; j < _airImages.Count; j++)
		{
			_airImages[j].gameObject.SetActive(value: false);
		}
		static Color ColorForOuterAnglecock(float value)
		{
			if (!((double)value < 0.01))
			{
				return Color.white;
			}
			return Color.clear;
		}
	}

	private void RemoveAllImages()
	{
		foreach (Image carImage in _carImages)
		{
			Object.Destroy(carImage.gameObject);
		}
		foreach (Image airImage in _airImages)
		{
			Object.Destroy(airImage.gameObject);
		}
		_carImages.Clear();
		_airImages.Clear();
	}

	private Color ColorForCar(Car car)
	{
		if (car.IsDerailed)
		{
			return derailedColor;
		}
		CarAirSystem air = car.air;
		if (air.handbrakeApplied)
		{
			return handbrakeAppliedColor;
		}
		return ColorForPsi(air.BrakeCylinder.Pressure);
	}

	private Color ColorForPsi(float psi)
	{
		return brakeCylinderGradient.Evaluate(Mathf.InverseLerp(0f, 72f, psi));
	}

	private Image GetAirImage(int index, float x, float y)
	{
		Image image;
		if (index >= _airImages.Count)
		{
			GameObject obj = new GameObject();
			obj.transform.SetParent(base.transform, worldPositionStays: false);
			obj.name = $"Joint {index}";
			obj.AddComponent<RectTransform>().SetFrame(x, y, _imageWidth, 6f);
			image = obj.AddComponent<Image>();
			image.type = Image.Type.Sliced;
			image.sprite = airIssueTile;
			_airImages.Add(image);
		}
		else
		{
			image = _airImages[index];
			image.gameObject.SetActive(value: true);
		}
		return image;
	}

	private Image GetCarImage(int index, float xCyl, float yCyl, bool isLocomotive)
	{
		Image image;
		if (_carImages.Count - 1 < index)
		{
			GameObject obj = new GameObject();
			obj.transform.SetParent(base.transform, worldPositionStays: false);
			obj.name = $"Car {index}";
			obj.AddComponent<RectTransform>().SetFrame(xCyl, yCyl, _imageWidth, 12f);
			image = obj.AddComponent<Image>();
			image.type = Image.Type.Sliced;
			_carImages.Add(image);
		}
		else
		{
			image = _carImages[index];
			image.gameObject.SetActive(value: true);
		}
		image.sprite = (isLocomotive ? locomotiveTile : carTile);
		return image;
	}
}
