using System;
using UnityEngine;

namespace UI.Tutorial;

[CreateAssetMenu(fileName = "Tutorial Node", menuName = "Railroader/Tutorial Node", order = 0)]
public class TutorialNode : ScriptableObject
{
	[Serializable]
	public struct Link
	{
		public string text;

		public TutorialNode target;
	}

	public string title;

	[TextArea(5, 50)]
	public string text;

	public Link[] links;
}
