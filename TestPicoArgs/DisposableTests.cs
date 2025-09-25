namespace TestPicoArgs;

using PicoArgs_dotnet;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0053 // Use expression body for lambda expression
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
#pragma warning disable RCS1021 // Convert lambda expression body to expression body.

public class DisposableTests
{
	[Fact(DisplayName = "PicoArgsDisposable check")]
	public void DisposalCheck()
	{
#pragma warning disable CA2000 // Dispose objects before losing scope
		var pico = SplitArgs.BuildDisposableFromSingleString("-f file.txt --something");
		var something = pico.Contains("--something");

		Helpers.AssertPicoThrows(() => {
			pico.Dispose();
		}, "Dispose() should throw when parameters are leftover", ErrorCode.UnrecognisedParameters);
#pragma warning restore CA2000 // Dispose objects before losing scope
	}
}
