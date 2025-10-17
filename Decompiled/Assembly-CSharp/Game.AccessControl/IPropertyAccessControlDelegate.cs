namespace Game.AccessControl;

public interface IPropertyAccessControlDelegate
{
	AuthorizationRequirementInfo AuthorizationRequirementForPropertyWrite(string key);
}
