using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;

namespace TypeD.Code
{
   /// <summary>
   /// Provides an abstract base class for generating and managing code files, including properties, functions, and interfaces.
   /// </summary>
   /// <remarks>The <see cref="Codalyzer"/> class is designed to facilitate the creation of structured code
   /// files with support for namespaces, classes, properties, functions, and interfaces. It provides methods for adding
   /// code elements, managing file content, and saving the generated code to a specified file path. Derived classes
   /// must implement the <see cref="InitClass"/> method to define specific initialization logic for the generated
   /// class.</remarks>
    public abstract class Codalyzer
    {
        // Class Definitions
        /// <summary>
        /// Provides functionality for writing formatted code to a target file, including support for indentation and
        /// managing code block delimiters.
        /// </summary>
        /// <remarks>The <see cref="CodeWriter"/> class is designed to simplify the process of generating 
        /// structured code by managing indentation levels and appending lines to a target file. It supports operations
        /// such as adding lines with indentation, appending raw text, and managing opening and closing curly brackets
        /// for code blocks.</remarks>
        public class CodeWriter
        {
            // Statics
            private static string Tab { get { return "    "; } }

            // Properties
            /// <summary>
            /// Gets or sets the target code file to write code to. 
            /// </summary>
            public CodeFile TargetFile { get; set; }
            /// <summary>
            /// Gets or sets the number of tabs currently open in the application.
            /// </summary>
            public int Tabs { get; set; }
            /// <summary>
            /// Gets or sets the skew value applied to tab alignment.
            /// </summary>
            public int TabSkew { get; set; }

            // Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="CodeWriter"/> class.
            /// </summary>
            public CodeWriter()
            {
            }

            // Functions
            /// <summary>
            /// Appends a line of text to the target file's output, optionally increasing the indentation level.
            /// </summary>
            /// <param name="line">The line of text to append. If not specified, an empty line is added.</param>
            /// <param name="tab">A value indicating whether to increase the indentation level before appending the line.</param>
            public void AddLine(string line = "", bool tab = false)
            {
                if (tab) Tabs++;
                TargetFile.Output.Append($"{string.Concat(Enumerable.Repeat(Tab, Tabs + TabSkew))}{line}{Environment.NewLine}");
            }

            /// <summary>
            /// Appends the specified line to the target file's output.
            /// </summary>
            /// <param name="line">The line of text to append. If not specified, an empty line is appended.</param>
            public void AppendLine(string line = "")
            {
                TargetFile.Output.Append(line);
            }

            /// <summary>
            /// Adds a left curly bracket ("{") to the current line and increases the indentation level.
            /// </summary>
            /// <remarks>This method appends a left curly bracket to the current line and increments
            /// the indentation level to ensure subsequent lines are properly indented. Use this method when starting a
            /// new block of code or scope.</remarks>
            public void AddLeftCurlyBracket()
            {
                AddLine("{");
                Tabs++;
            }

            /// <summary>
            /// Adds the specified number of closing curly brackets ('}') to the output,  reducing the indentation level
            /// for each bracket added.
            /// </summary>
            /// <remarks>This method ensures that the number of closing curly brackets added does not 
            /// exceed the current indentation level. Each added bracket decreases the indentation level by
            /// one.</remarks>
            /// <param name="count">The number of closing curly brackets to add. Defaults to 1. If the specified count exceeds the current
            /// indentation level, only the available indentation levels will be reduced.</param>
            public void AddRightCurlyBrackets(int count = 1)
            {
                count = (count < Tabs ? count : Tabs);
                for (int i = 0; i < count; i++)
                {
                    if (Tabs > 0)
                        Tabs--;
                    AddLine("}");
                }
            }

            /// <summary>
            /// Adds all necessary closing brackets to the current context.
            /// </summary>
            /// <remarks>This method ensures that the appropriate number of closing curly brackets are
            /// added based on the current indentation level or context.</remarks>
            public void AddAllClosingBrackets()
            {
                AddRightCurlyBrackets(Tabs);
            }

            /// <summary>
            /// Resets the state of the object by clearing all tab-related values and the output content.
            /// </summary>
            /// <remarks>This method sets the tab count and skew to zero and clears the content of the
            /// associated target file. Use this method to reset the object to its initial state.</remarks>
            public void Clear()
            {
                Tabs = 0;
                TabSkew = 0;
                TargetFile.Output.Clear();
            }
        }

        /// <summary>
        /// Represents a function with a definition and a body that can be generated into code.
        /// </summary>
        /// <remarks>The <see cref="Function"/> class allows defining a function with a string-based
        /// definition (e.g., a function signature) and a body represented as an action that writes the function's
        /// implementation to a <see cref="CodeWriter"/>. This class provides functionality to generate the complete
        /// function code, including its definition and body, in a structured format.</remarks>
        public class Function
        {
            // Properties
            /// <summary>
            /// Gets or sets the definition or description associated with the current <see cref="Function"/>.
            /// </summary>
            public string Definition { get; set; }

            /// <summary>
            /// Gets or sets the action that defines the body of the code to be written.
            /// </summary>
            /// <remarks>Use this property to provide a custom implementation for generating the body
            /// of the code. The action should define how the <see cref="CodeWriter"/> is used to produce the desired
            /// output.</remarks>
            public Action<CodeWriter> Body { get; set; }

            // Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="Function"/> class with the specified definition and body.
            /// </summary>
            /// <param name="definition">The definition of the function, typically representing its signature or declaration.</param>
            /// <param name="body">An action that writes the body of the function using a <see cref="CodeWriter"/>.</param>
            public Function(string definition, Action<CodeWriter> body)
            {
                Definition = definition;
                Body = body;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Function"/> class with the specified definition and body.
            /// </summary>
            /// <param name="definition">The definition of the function, typically describing its purpose or behavior.</param>
            /// <param name="body">The action to be executed when the function is invoked. This action does not take any parameters.</param>
            public Function(string definition, Action body)
            {
                Definition = definition;
                Body = (_) => { body(); };
            }

            // Functions
            /// <summary>
            /// Generates the code representation of the current object and writes it to the specified <see cref="CodeWriter"/>.
            /// </summary>
            /// <remarks>This method writes the object's definition, encloses the body within curly
            /// brackets, and delegates the body content generation to the Body method. Ensure that the
            /// <paramref name="writer"/> is properly initialized before calling this method.</remarks>
            /// <param name="writer">The <see cref="CodeWriter"/> instance to which the generated code will be written. This parameter cannot
            /// be <see langword="null"/>.</param>
            public void Generate(CodeWriter writer)
            {
                writer.AddLine(Definition);
                writer.AddLeftCurlyBracket();
                Body(writer);
                writer.AddRightCurlyBrackets();
            }
        }

        /// <summary>
        /// Represents a property definition with an optional body for generating code.
        /// </summary>
        /// <remarks>This class allows defining a property with a string representation of its definition
        /// and an optional body that can be used to generate additional code, such as custom accessors or other logic.
        /// The body is represented as an action that writes to a <see cref="CodeWriter"/>.</remarks>
        public class Property
        {
            // Properties
            /// <summary>
            /// Gets or sets the definition or description associated with the current <see cref="Property"/>.
            /// </summary>
            public string Definition { get; set; }

            /// <summary>
            /// Gets or sets the action that defines the body of the code to be written.
            /// </summary>
            /// <remarks>The <see cref="Body"/> property allows customization of the code generation
            /// process by specifying an action that writes the desired content to a <see cref="CodeWriter"/> instance.
            /// This property must be set before invoking any operation that requires the code body to be
            /// generated.</remarks>
            public Action<CodeWriter> Body { get; set; }

            // Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="Property"/> class with the specified definition.
            /// </summary>
            /// <param name="definition">The definition of the property, representing its declaration or signature.</param>
            public Property(string definition)
            {
                Definition = definition;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Property"/> class with the specified definition and body
            /// action.
            /// </summary>
            /// <remarks>The <paramref name="body"/> parameter is wrapped in a delegate that takes a
            /// single parameter,  but the parameter is not used in the execution of the action.</remarks>
            /// <param name="definition">The definition of the property, representing its declaration or signature.</param>
            /// <param name="body">An action that represents the body of the property. This action is executed when the property is
            /// invoked.</param>
            public Property(string definition, Action body)
            {
                Definition = definition;
                Body = (_) => { body(); };
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="Property"/> class with the specified definition and body.
            /// </summary>
            /// <param name="definition">The definition of the property, representing its declaration or signature.</param>
            /// <param name="body">An action that writes the body of the property using a <see cref="CodeWriter"/>.</param>
            public Property(string definition, Action<CodeWriter> body)
            {
                Definition = definition;
                Body = body;
            }

            // Functions
            /// <summary>
            /// Generates the code representation of the current object and writes it to the specified <see cref="CodeWriter"/>.
            /// </summary>
            /// <remarks>If the Body property is <see langword="null"/>, a single-line property
            /// definition is written. Otherwise, the full property definition, including its body, is written with
            /// proper formatting.</remarks>
            /// <param name="writer">The <see cref="CodeWriter"/> instance to which the generated code will be written. Cannot be <see langword="null"/>.</param>
            public void Generate(CodeWriter writer)
            {
                if (Body == null)
                {
                    writer.AddLine($"{Definition} {{ get; set; }}");
                }
                else
                {
                    writer.AddLine(Definition);
                    writer.AddLeftCurlyBracket();
                    Body(writer);
                    writer.AddRightCurlyBrackets();
                }
            }
        }

        /// <summary>
        /// Represents a code file, including its file path, content, and associated metadata such as functions,
        /// properties, interfaces, and using directives.
        /// </summary>
        /// <remarks>This class provides functionality to define and manage the structure of a code file,
        /// including its content and metadata. It supports saving the file to disk and dynamically generating using
        /// directives.</remarks>
        public class CodeFile
        {
            // Properties
            /// <summary>
            /// Gets or sets the file path associated with the operation.
            /// </summary>
            public string FilePath { get; set; }
            /// <summary>
            /// Gets or sets the output data.
            /// </summary>
            public StringBuilder Output { get; set; }
            /// <summary>
            /// Gets or sets the list of interface types associated with the current code.
            /// </summary>
            public List<Type> Interfaces { get; set; }
            /// <summary>
            /// Gets or sets the collection of functions available in the system.
            /// </summary>
            public List<Function> Functions { get; set; }
            /// <summary>
            /// Gets or sets the collection of properties associated with the code.
            /// </summary>
            public List<Property> Properties { get; set; }
            /// <summary>
            /// Gets or sets the list of namespaces or assemblies used in the current context.
            /// </summary>
            public List<string> Usings { get; set; }
            /// <summary>
            /// Gets or sets a delegate that dynamically provides a list of strings to be used in the operation.
            /// </summary>
            /// <remarks>The delegate can be used to supply context-specific or dynamically computed
            /// data at runtime. Ensure the function does not return <see langword="null"/> to avoid potential runtime
            /// issues.</remarks>
            public Func<List<string>> DynamicUsing { get; set; }
            /// <summary>
            /// Gets or sets a value indicating whether the file is automatically generated.
            /// </summary>
            public bool AutoGeneratedFile { get; set; }

            // Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="CodeFile"/> class.
            /// </summary>
            /// <remarks>This constructor initializes the <see cref="Output"/> property as an empty
            /// StringBuilder, and the <see cref="Functions"/>, <see cref="Properties"/>, <see
            /// cref="Interfaces"/>, and <see cref="Usings"/> properties as empty collections.</remarks>
            public CodeFile()
            {
                Output = new StringBuilder();
                Functions = new List<Function>();
                Properties = new List<Property>();
                Interfaces = new List<Type>();
                Usings = new List<string>();
            }

            // Functions
            /// <summary>
            /// Saves the current output to the specified file path.
            /// </summary>
            /// <remarks>This method writes the content of the <see cref="Output"/> property to the
            /// file specified by <see cref="FilePath"/>. If the directory for the file does not exist, it will be
            /// created. The method does nothing if <see cref="Output"/> is <see langword="null"/> or empty.</remarks>
            public void Save()
            {
                if (Output != null && Output.Length > 0)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                    File.WriteAllText(FilePath, Output.ToString());
                }
            }
        }

        // Statics
        /// <summary>
        /// Gets the auto-generated text message indicating that the file was generated by a tool and should not be modified.
        /// </summary>
        protected static string AutoGeneratedText
        {
            get
            {
                return
@"/* This file have been autogenerated by TypeD
** Do not change any of it's content */";
            }
        }

        // Properties
        /// <summary>
        /// Gets the project associated with the current code.
        /// </summary>
        public Project Project { get; internal set; }
        /// <summary>
        /// Gets the collection of resources associated with the current code.
        /// </summary>
        public IResourceModel Resources { get; internal set; }
        /// <summary>
        /// Gets the <see cref="CodeWriter"/> instance used for generating code output.
        /// </summary>
        /// <remarks>This property is read-only outside the class and is intended for use in derived
        /// classes to facilitate code generation tasks.</remarks>
        protected CodeWriter Writer { get; private set; }
        /// <summary>
        /// Gets or sets the base file associated with the current code.
        /// </summary>
        protected CodeFile BaseFile { get; set; }
        /// <summary>
        /// Gets or sets the namespace associated with the current code.
        /// </summary>
        public string Namespace { get; set; }
        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// Gets or sets the name of the base class associated with the current code.
        /// </summary>
        public string BaseClass { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the class is marked as partial.
        /// </summary>
        public bool PartialClass { get; set; }
        /// <summary>
        /// Gets a value indicating whether the object has been successfully initialized.
        /// </summary>
        public bool Initialized { get; private set; }

        // Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Codalyzer"/> class.
        /// </summary>
        /// <remarks>This constructor initializes the <see cref="Codalyzer"/> instance with default
        /// values: <list type="bullet"> <item><description><see cref="PartialClass"/> is set to "false".
        /// </description></item> <item><description><see cref="Writer"/> is initialized as a new
        /// instance of <see cref="CodeWriter"/>.</description></item> <item><description><see cref="BaseFile"/> is
        /// initialized as a new instance of <see cref="CodeFile"/>.</description></item> </list></remarks>
        public Codalyzer()
        {
            PartialClass = false;
            Writer = new CodeWriter();
            BaseFile = new CodeFile();
        }

        /// <summary>
        /// Initializes the current instance, setting up necessary dependencies and state.
        /// </summary>
        /// <remarks>This method configures the writer's target file, initializes the class, and sets the
        /// base file's file path. After calling this method, the instance is marked as initialized.</remarks>
        public virtual void Init()
        {
            Writer.TargetFile = BaseFile;
            InitClass();
            BaseFile.FilePath = FilePath();
            Initialized = true;
        }

        /// <summary>
        /// Initializes the class by performing any required setup or configuration.
        /// </summary>
        /// <remarks>This method must be implemented by derived classes to define the specific
        /// initialization logic. It is called during the construction phase of the class.</remarks>
        protected abstract void InitClass();

        // Functions
        /// <summary>
        /// Adds a function to the collection of functions in the target file.
        /// </summary>
        /// <param name="function">The function to add. Cannot be <see langword="null"/>.</param>
        public void AddFunction(Function function)
        {
            Writer.TargetFile.Functions.Add(function);
        }

        /// <summary>
        /// Adds the specified interface to the target file's list of interfaces.
        /// </summary>
        /// <param name="interface">The <see cref="Type"/> representing the interface to add. Cannot be <see langword="null"/>.</param>
        public void AddInterface(Type @interface)
        {
            Writer.TargetFile.Interfaces.Add(@interface);
        }

        /// <summary>
        /// Adds a property to the collection of properties in the target file.
        /// </summary>
        /// <param name="property">The property to add. Cannot be <see langword="null"/>.</param>
        public void AddProperty(Property property)
        {
            Writer.TargetFile.Properties.Add(property);
        }

        /// <summary>
        /// Adds the specified list of using directives to the target file.
        /// </summary>
        /// <remarks>This method appends the provided using directives to the existing collection of using
        /// directives in the target file. Duplicate entries are not automatically removed, so ensure the list does not
        /// contain duplicates if uniqueness is required.</remarks>
        /// <param name="usings">A list of using directives to add. Each entry should represent a valid namespace or alias.</param>
        public void AddUsings(List<string> usings)
        {
            Writer.TargetFile.Usings.AddRange(usings);
        }

        /// <summary>
        /// Adds a using directive to the target file.
        /// </summary>
        /// <remarks>This method appends the specified namespace to the collection of using directives for
        /// the target file. Ensure that the namespace is valid and not already present to avoid duplicates.</remarks>
        /// <param name="using">The namespace to be added as a using directive. Cannot be <see langword="null"/> or empty.</param>
        public void AddUsing(string @using)
        {
            Writer.TargetFile.Usings.Add(@using);
        }

        /// <summary>
        /// Sets a delegate that provides a dynamic list of using directives.
        /// </summary>
        /// <remarks>The provided delegate is invoked to retrieve the list of using directives when
        /// needed. Ensure the function returns a valid list of strings, as these will be used in the target file
        /// generation process.</remarks>
        /// <param name="dynamicUsing">A function that returns a list of strings representing the using directives to be dynamically applied.</param>
        public void SetDynamicUsing(Func<List<string>> dynamicUsing)
        {
            Writer.TargetFile.DynamicUsing = dynamicUsing;
        }

        /// <summary>
        /// Retrieves a list of interfaces implemented by the target type.
        /// </summary>
        /// <remarks>This method provides access to the interfaces associated with the target type defined
        /// in the writer's target file.</remarks>
        /// <returns>A list of objects representing the interfaces implemented by the target type. The list
        /// will be empty if no interfaces are implemented.</returns>
        public List<Type> GetInterfaces()
        {
            return Writer.TargetFile.Interfaces;
        }

        /// <summary>
        /// Generates the output file by writing content to the target file.
        /// </summary>
        /// <remarks>This method clears the current content of the target file and writes new content to it.
        /// The target file is determined by the Writers <see cref="CodeWriter.TargetFile"/> property.</remarks>
        public virtual void Generate()
        {
            Writer.TargetFile = BaseFile;
            Writer.Clear();
            WriteFile();
        }

        /// <summary>
        /// Generates and writes the content of a class file, including namespaces, usings, inheritance, properties, and
        /// methods, to the target writer.
        /// </summary>
        /// <remarks>This method constructs the class file dynamically based on the provided target file's
        /// metadata, such as base class, interfaces, properties, and functions. It ensures that necessary `using`
        /// directives are included, handles namespace declarations, and writes the class definition with appropriate
        /// inheritance and members. If the target file is marked as auto-generated, a corresponding comment is added at
        /// the top of the file.</remarks>
        public void WriteFile()
        {
            var inheritance = new List<string>();
            if (!string.IsNullOrEmpty(BaseClass))
            {
                inheritance.Add(BaseClass.Split(".").LastOrDefault());
            }
            inheritance.AddRange(Writer.TargetFile.Interfaces.Select((i) => { return i.Name; }));

            if (Writer.TargetFile.AutoGeneratedFile)
            {
                Writer.AddLine(AutoGeneratedText);
                Writer.AddLine();
            }

            var dynamicUsings = new List<string>();
            if(Writer.TargetFile.DynamicUsing != null)
            {
                dynamicUsings = Writer.TargetFile.DynamicUsing();
            }
            dynamicUsings.AddRange(Writer.TargetFile.Interfaces.Select((i) => { return i.Namespace; }));
            if (!string.IsNullOrEmpty(BaseClass) && BaseClass.Contains('.'))
            {
                dynamicUsings.Add(string.Join('.', BaseClass.Split('.').SkipLast(1)));
            }
            var usings = Writer.TargetFile.Usings.Union(dynamicUsings).Distinct().Where(u => u != Namespace && u != "").ToList();
            foreach (var @using in usings)
            {
                Writer.AddLine($"using {@using};");
            }
            Writer.AddLine();
            Writer.AddLine($"namespace {Namespace}");
            Writer.AddLeftCurlyBracket();
            Writer.AddLine($"{(PartialClass? "partial ":"")}class {ClassName}{(inheritance.Count == 0 ? "" : $" : {string.Join(", ", inheritance)}")}");
            Writer.AddLeftCurlyBracket();

            if(Writer.TargetFile.Properties.Count > 0)
                Writer.AddLine("// Properties");
            foreach(var property in Writer.TargetFile.Properties)
            {
                property.Generate(Writer);
            }

            if (Writer.TargetFile.Functions.Count > 0)
            {
                if (Writer.TargetFile.Properties.Count > 0)
                    Writer.AddLine();
                Writer.AddLine("// Functions");
            }
            foreach (var function in Writer.TargetFile.Functions)
            {
                function.Generate(Writer);
                if(Writer.TargetFile.Functions.LastOrDefault() != function)
                    Writer.AddLine();
            }

            Writer.AddAllClosingBrackets();
        }

        /// <summary>
        /// Saves the current state of the file to disk.
        /// </summary>
        /// <remarks>If the file is not marked as auto-generated and already exists at the specified file
        /// path, the save operation is skipped. Otherwise, the file is saved using the base file's save
        /// logic.</remarks>
        public virtual void Save()
        {
            if (!BaseFile.AutoGeneratedFile && File.Exists(FilePath()))
            {
                return;
            }

            BaseFile.Save();
        }

        /// <summary>
        /// Constructs the file path for the source file based on the project location, namespace, and class name.
        /// </summary>
        /// <returns>A string representing the full file path of the source file. The path is constructed by combining the
        /// project location, the namespace (with dots replaced by directory separators), and the class name with a
        /// ".cs" extension.</returns>
        public string FilePath()
        {
            return Path.Combine(Project.Location, Namespace.Replace('.', '\\'), $"{ClassName}.cs");
        }
    }
}
