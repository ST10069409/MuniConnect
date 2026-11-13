using MuniConnect.Core.Services;

namespace MuniConnect.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.Classic);
        var repository = new CivicRepository();
        repository.Init();
        Application.Run(new ShellForm(repository));
    }
}
