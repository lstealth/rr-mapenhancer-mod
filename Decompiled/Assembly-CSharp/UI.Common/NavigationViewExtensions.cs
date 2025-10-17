namespace UI.Common;

public static class NavigationViewExtensions
{
	public static NavigationController NavigationController(this INavigationView navigationView)
	{
		return navigationView.RectTransform.GetComponentInParent<NavigationController>();
	}
}
