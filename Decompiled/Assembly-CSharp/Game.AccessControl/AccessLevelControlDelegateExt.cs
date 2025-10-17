namespace Game.AccessControl;

public static class AccessLevelControlDelegateExt
{
	public static IPropertyAccessControlDelegate StaticDelegate(this AuthorizationRequirementInfo a)
	{
		return new StaticPropertyAccessControlDelegate(a);
	}
}
