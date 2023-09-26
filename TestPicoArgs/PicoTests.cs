namespace TestPicoArgs;

public class PicoTests
{
	[Fact(DisplayName = "Example: test", Timeout = 200)]
	[Trait("Category", "Test1")]
	public void Test1()
	{
		Assert.True(true);
	}
}