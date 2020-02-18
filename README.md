# Console Out Observer

Capture and observe Console output written by methods such as Console.WriteLine.
It also leaves the original console output stream intact.

This is useful for temporarily capturing logging during integration or unit
testing of your own code.

This is NOT useful for [permanent silencing of Console.Out](https://stackoverflow.com/a/1412303).

This is NOT useful for capturing output of an externally started program. For
that you will want to [look into usage](https://stackoverflow.com/questions/12678407/getting-command-line-output-dynamically/12678433)
of [`System.Diagnostics.Process`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process?view=netcore-3.1).

## Installing / Getting started

1. [Install the Captin.ConsoleIntercept NuGet package](https://www.nuget.org/packages/Captin.ConsoleIntercept/)
2. Add the NuGet package to your project
3. Observe Console output in a test
	```csharp
	using Captin.ConsoleIntercept;
	using Xunit;

	[Fact]
	public void Test1()
	{
		System.Console.Write("not observed");
		using(var logged = ConsoleOut.Observe()) {
			System.Console.Write("log (sent to real console too)");
			Assert.Equal("log (sent to real console too)", logged.ToString());
		}
		System.Console.Write("not observed (and real console is restored)");
	}
	```

## Features

* Capture console output cleanly with a [using statement](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement)
	```csharp
	using(var logged = ConsoleOut.Observe()) {
		...
	}
	```
* Nested `using` statements supported
	```csharp
	using(var a = ConsoleOut.Observe()) {
		Console.WriteLine("a");
		using(var b = ConsoleOut.Observe()) {
			Console.WriteLine("a and b");
		}
	}
	```
* Leaves standard `Console.Out` intact even when observing
	```csharp
	using(var logged = ConsoleOut.Observe()) {
		Console.WriteLine("'logged' (and original console too)");
	}
	```
* NOT thread safe.  Running tests in parallel that both capture output may cause
  observing logs from `Test1` to show up in `Test2`

## Developing

Here's a brief intro about what a developer must do in order to start developing
the project further:

1. Clone the project
	```powershell
	git clone https://github.com/captin411/Captin.ConsoleIntercept.git
	```
2. Open the solution in Visual Studio.

## Contributing

Pull requests are welcome.

If you want to contribute, please fork the repository and use a feature branch.

It would be AWESOME if you add in a test to cover any new behavior or bug fixes
that you might have.

## Links

* Project: https://github.com/captin411/Captin.ConsoleIntercept
* Nuget Package: https://www.nuget.org/packages/Captin.ConsoleIntercept/

## Licensing

The code in this project is licensed under MIT license.
