using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace QModReloadedInstaller;

public class Injector
{
    private const string InjectorFile = "QModReloadedInstaller.dll";
    private readonly string _mainFilename = "\\Assembly-CSharp.dll";
    private readonly string _backupFilename = "\\Assembly-CSharp.dll.original";

    public Injector(string gamePath)
    {
        var managedDirectory = Path.Combine(gamePath, "Graveyard Keeper_Data\\Managed");
        _mainFilename = managedDirectory + _mainFilename;
        _backupFilename = managedDirectory + _backupFilename;
    }

    public (bool injected, string message) InjectNoIntros()
    {
        try
        {
            if(IsNoIntroInjected()) return (true,
                $"Intros patch already injected.");
            var gameAssembly = AssemblyDefinition.ReadAssembly(_mainFilename);

            var logoScene = gameAssembly.MainModule.GetType("LogoScene");
            var awakeMethod = logoScene.Methods.First(x => x.Name == "Awake");

            //where the original instruction comes from
            var onFinishedMethod = logoScene.Methods.First(x => x.Name == "OnFinished");

            awakeMethod.Body.Method.Body = onFinishedMethod.Body.Method.Body;
            gameAssembly.Write(_mainFilename);
            return (true,
                $"Intros patch injected : {onFinishedMethod.Body.Instructions[0].Operand} inserted into {awakeMethod}");
        }
        catch (Exception ex)
        {
            return (false, $"Intros patch injected ERROR: {ex.Message}");
        }
    }

    public (bool injected, string message) Inject()
    {
        try
        {
            if (IsInjected()) return (true, $"Mod patch already applied.");
            var gameAssembly = AssemblyDefinition.ReadAssembly(_mainFilename);
            var injectorAssembly = AssemblyDefinition.ReadAssembly(InjectorFile);
            var patchInstruction = injectorAssembly.MainModule.GetType("QModReloadedInstaller.QModLoader").Methods
                .Single(x => x.Name == "Patch");
            var logoScene = gameAssembly.MainModule.GetType("LogoScene");
            var awakeMethod = logoScene.Methods.First(x => x.Name == "Awake");
            awakeMethod.Body.GetILProcessor().InsertBefore(awakeMethod.Body.Instructions[0],
                Instruction.Create(OpCodes.Call, awakeMethod.Module.Import(patchInstruction)));
            gameAssembly.Write(_mainFilename);
            return (true, $"Mod patch injected: {patchInstruction} inserted into {awakeMethod}");
        }
        catch (Exception ex)
        {
            return (false, $"Mod patch inject ERROR: {ex.Message}");
        }
    }

    public (bool removed, string message) Remove()
    {
        try
        {
            if (!IsInjected()) return (false, $"Mod patch already removed.");
            var gameAssembly = AssemblyDefinition.ReadAssembly(_mainFilename);
            var logoScene = gameAssembly.MainModule.GetType("LogoScene");
            var awakeMethod = logoScene.Methods.First(x => x.Name == "Awake");
            var processor = awakeMethod.Body.GetILProcessor();
            var logText = awakeMethod.Body.Instructions[0].Operand.ToString();
            processor.Remove(awakeMethod.Body.Instructions[0]);
            gameAssembly.Write(_mainFilename);
            return(true, $"Mod patch removed: {logText} removed from {awakeMethod}");
        }
        catch (Exception ex)
        {
            return(false, $"Mod patch remove ERROR: {ex.Message}");
        }
    }
    
    public bool IsInjected()
       {

        try
        {
            var gameAssembly = AssemblyDefinition.ReadAssembly(_mainFilename);
            var logoScene = gameAssembly.MainModule.GetType("LogoScene");
            var awakeMethod = logoScene.Methods.First(x => x.Name == "Awake");
            return awakeMethod.Body.Instructions.Any(instruction =>
                instruction.OpCode.Equals(OpCodes.Call) && instruction.Operand.ToString()
                    .Equals("System.Void QModReloadedInstaller.QModLoader::Patch()", StringComparison.Ordinal));
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"IsInjected ERROR: {ex.Message}");
        }

        return false;
       }

    public bool IsNoIntroInjected()
    {

        try
        {
            var gameAssembly = AssemblyDefinition.ReadAssembly(_mainFilename);
            var logoScene = gameAssembly.MainModule.GetType("LogoScene");

            //check for start preload instruction in awake method
            var awakeMethod = logoScene.Methods.First(x => x.Name == "Awake");

            return awakeMethod.Body.Instructions.Any(instruction =>
                instruction.OpCode.Equals(OpCodes.Call) && instruction.Operand.ToString()
                    .Equals("System.Void UnityEngine.SceneManagement.SceneManager::LoadScene(System.String)", StringComparison.Ordinal));
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"IsNoIntroInjected ERROR: {ex.Message}");
        }

        return false;


    }
}