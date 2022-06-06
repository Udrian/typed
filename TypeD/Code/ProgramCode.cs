﻿using System.Collections.Generic;

namespace TypeD.Code
{
    public class ProgramCode : Codalyzer
    {
        // Constructors
        public ProgramCode() : base()
        {
        }

        public override void Init()
        {
            ClassName = "Program";
            Namespace = Project.ProjectName;
            BaseFile.AutoGeneratedFile = true;

            base.Init();
        }

        protected override void InitClass()
        {
            AddUsings(new List<string>()
            {
                "TypeOEngine.Typedeaf.Core.Engine"
            });

            SetDynamicUsing(() =>
            {
                var usings = new List<string>();

                foreach (var module in Project.Modules)
                {
                    var moduleType = module.ModuleTypeInfo;
                    if (moduleType == null || moduleType.Name == "TypeOCore" || module.IsTypeD) continue;
                    if (module.ModuleTypeInfo != null)
                        usings.Add(module.ModuleTypeInfo.Namespace);
                }

                return usings;
            });

            AddFunction(new Function($"static void Main()", () =>
            {
                Writer.AddLine($"TypeO.Create<{Project.ProjectName}Game>(\"{Project.ProjectName}\")");
                foreach (var module in Project.Modules)
                {
                    var moduleType = module.ModuleTypeInfo;
                    if (moduleType == null || moduleType.Name == "TypeOCore" || module.IsTypeD) continue;
                    Writer.AddLine($".LoadModule<{moduleType.Name}>()");
                }
                Writer.AddLine(".Start();", true);
                Writer.Tabs--;
            }));
        }
    }
}