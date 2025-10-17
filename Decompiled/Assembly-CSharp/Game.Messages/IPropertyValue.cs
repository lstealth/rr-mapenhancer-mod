using MessagePack;

namespace Game.Messages;

[Union(1, typeof(NullPropertyValue))]
[Union(2, typeof(BoolPropertyValue))]
[Union(3, typeof(FloatPropertyValue))]
[Union(4, typeof(IntPropertyValue))]
[Union(5, typeof(StringPropertyValue))]
[Union(6, typeof(ArrayPropertyValue))]
[Union(7, typeof(DictionaryPropertyValue))]
public interface IPropertyValue
{
}
