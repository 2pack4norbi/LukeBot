using System;
using System.IO;
using LukeBot.Common;
using LukeBot.Config;
using LukeBot.Logging;
using CommandLine;

namespace propmgr;

public class Command
{
    [Option('d', "dir",
        Required = false,
        Default = Constants.PROPERTY_STORE_FILE,
        HelpText = "Property Store file to use.")
    ]
    public string StoreDir { get; set; }

    public Command()
    {
        StoreDir = "";
    }
}

[Verb("create", HelpText = "Create a new empty Property Store. Overwrites old one if exists.")]
public class CreateCommand: Command
{
    [Option('t', "template",
        Required = false,
        Default = "default",
        HelpText = "Template to use when creating the Store. Available options: default, empty")]
    public string TemplateType { get; set; }

    public CreateCommand()
    {
        TemplateType = "";
    }
}

[Verb("add", HelpText = "Add a new Property to the store.")]
public class AddCommand: Command
{
    [Value(0, MetaName = "prop_name", Required = true, HelpText = "Name of property to add.")]
    public string Name { get; set; }
    [Value(1, MetaName = "prop_type", Required = true, HelpText = "Type of added property.")]
    public string Type { get; set; }
    [Value(2, MetaName = "prop_value", Required = true, HelpText = "Value of added property.")]
    public string Value { get; set; }

    public AddCommand()
    {
        Name = "";
        Type = "";
        Value = "";
    }
}

[Verb("remove", HelpText = "Remove a property.")]
public class RemoveCommand: Command
{
    [Value(0, MetaName = "prop_name", Required = true, HelpText = "Name of property to add.")]
    public string Name { get; set; }

    public RemoveCommand()
    {
        Name = "";
    }
}

[Verb("edit", HelpText = "Modify an existing property. Type must match with existing property's type.")]
public class ModifyCommand: Command
{
    [Value(0, MetaName = "prop_name", Required = true, HelpText = "Name of property to add.")]
    public string Name { get; set; }
    [Value(1, MetaName = "prop_value", Required = true, HelpText = "Value of added property.")]
    public string Value { get; set; }

    public ModifyCommand()
    {
        Name = "";
        Value = "";
    }
}

[Verb("list", HelpText = "List all existing properties in the Store.")]
public class ListCommand: Command
{
    [Option('a', "all",
        Required = false,
        Default = false,
        HelpText = "Enable option to list hidden properties (might print out unsafe properties)")]
    public bool ShowAll { get; set; }

    public ListCommand()
    {
    }
}

public class CommandProcessor
{
    private PropertyStore OpenStore(Command cmd)
    {
        PropertyStore store = new PropertyStore(cmd.StoreDir);
        return store;
    }

    private string PrepareType(string inType)
    {
        switch (inType)
        {
        case "sbyte": return "System.SByte";
        case "byte": return "System.Byte";
        case "short": return "System.Int16";
        case "ushort": return "System.UInt16";
        case "int": return "System.Int32";
        case "uint": return "System.UInt32";
        case "long": return "System.Int64";
        case "ulong": return "System.UInt64";
        case "nint": return "System.IntPtr";
        case "nuint": return "System.UIntPtr";
        case "float": return "System.Single";
        case "double": return "System.Double";
        case "decimal": return "System.Decimal";
        case "bool": return "System.Boolean";
        case "char": return "System.Char";
        case "string": return "System.String";
        default: return inType; // probably custom type, leave unchanged
        }
    }

    public void CreatePropertyStore(CreateCommand cmd)
    {
        Logger.Log().Info("Creating Property Store at {0} with template {1}", cmd.StoreDir, cmd.TemplateType);

        if (FileUtils.Exists(cmd.StoreDir))
        {
            Logger.Log().Warning("File {0} already exists. Overwrite? (y/n)", cmd.StoreDir);
            bool accepted = false;

            string? line = Console.ReadLine();

            if (line == null)
            {
                Logger.Log().Info("No option provided");
                return;
            }

            if (line.Length == 1 && (line[0] == 'y' || line[0] == 'n'))
            {
                accepted = (line[0] == 'y');
            }
            else
            {
                Logger.Log().Info("Option {0} incorrect - specify 'y' or 'n'", line);
                return;
            }

            if (!accepted)
            {
                Logger.Log().Info("Aborting.");
                return;
            }

            File.Delete(cmd.StoreDir);
        }

        PropertyStore store = new PropertyStore(cmd.StoreDir);
        StoreTemplate template = StoreTemplate.Select(cmd.TemplateType);
        template.Fill(store);
        store.Save();
    }

    public void AddProperty(AddCommand cmd)
    {
        string type = PrepareType(cmd.Type);
        Logger.Log().Info("Adding Property {0} {1} to store {2}", type, cmd.Name, cmd.StoreDir);
        PropertyStore store = OpenStore(cmd);
        store.Add(LukeBot.Config.Path.Parse(cmd.Name), Property.Create(type, cmd.Value));
        store.Save();
    }

    public void RemoveProperty(RemoveCommand cmd)
    {
        Logger.Log().Info("Removing Property {0} from store {1}", cmd.Name, cmd.StoreDir);
        PropertyStore store = OpenStore(cmd);
        store.Remove(LukeBot.Config.Path.Parse(cmd.Name));
        store.Save();
    }

    public void ModifyProperty(ModifyCommand cmd)
    {
        Logger.Log().Info("Modify Property {0} in store {1}", cmd.Name, cmd.StoreDir);
        PropertyStore store = OpenStore(cmd);
        store.Modify(LukeBot.Config.Path.Parse(cmd.Name), cmd.Value);
        store.Save();
    }

    public void ListProperties(ListCommand cmd)
    {
        Logger.Log().Info("List properties in store {0}", cmd.StoreDir);
        PropertyStore store = OpenStore(cmd);
        store.PrintDebug(LogLevel.Info, cmd.ShowAll);
    }
}
