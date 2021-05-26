using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using BrawlLib.SSBB.ResourceNodes;
namespace AIScriptCLA
{
    class Program
    {
        // taken directly from the AIScriptpad2.0.exe decompiled source
        private static T[] FindSomething<T>(ResourceNode node, ResourceType t) where T : ResourceNode
		{
			List<T> list = new List<T>();
			IEnumerable<ResourceNode> enumerable = node.Children.Where((ResourceNode x) => x.ResourceType == ResourceType.AISet || x.ResourceType == ResourceType.ARC);
			foreach (ResourceNode item in enumerable)
			{
				if (!item.Name.Contains("ai_"))
				{
					continue;
				}
				foreach (ResourceNode child in item.Children)
				{
					if (child.ResourceType == t)
					{
						list.Add(child as T);
					}
				}
			}
			if (list.Count > 0)
			{
				return list.ToArray();
			}
			foreach (ResourceNode item2 in enumerable)
			{
				T[] collection;
				if ((collection = FindSomething<T>(item2, t)) != null)
				{
					list.AddRange(collection);
				}
			}
			if (list.Count > 0)
			{
				return list.ToArray();
			}
			return new T[0];
		}

        static void Main(string[] args)
        {            
            if (args.Contains("--export")) {
                string path = "nop";
                string outFolder = "nop";
                string includeFolder = "nop";

                int idx = Array.FindIndex(args, item => item == "--path");
                if (idx != -1 && idx + 1 < args.Length) {
                    path = args[idx + 1];
                }

                idx = Array.FindIndex(args, item => item == "--out");
                if (idx != -1 && idx + 1 < args.Length) {
                    outFolder = args[idx + 1];
                }

                idx = Array.FindIndex(args, item => item == "--include");
                if (idx != -1 && idx + 1 < args.Length) {
                    includeFolder = args[idx + 1];
                }

                if (path != "nop" && outFolder != "nop" && includeFolder != "nop") {
                    BrawlAICore.Options.IncludePath = includeFolder;
                    ResourceNode node = NodeFactory.FromFile(null, path);
		            ARCEntryNode[] array = FindSomething<ARCEntryNode>(node, ResourceType.AIPD);
                    for (int i = 0; i < array.Length; i++) {
                        DirectoryInfo dir = Directory.CreateDirectory(outFolder + "/" + array[i].Parent.Name);
                        BrawlAICore.Exporter.Export(
                            path,
                            dir.FullName,
                            new System.Collections.Generic.List<string>(),
                            i
                        );
                    }
                }

            }

            if (args.Contains("--compile")) {
                string path = "nop";
                string outFolder = "nop";
                string includeFolder = "nop";
                string toReplace = "nop";

                int idx = Array.FindIndex(args, item => item == "--path");
                if (idx != -1 && idx + 1 < args.Length) {
                    path = args[idx + 1];
                }

                idx = Array.FindIndex(args, item => item == "--out");
                if (idx != -1 && idx + 1 < args.Length) {
                    outFolder = args[idx + 1];
                }

                idx = Array.FindIndex(args, item => item == "--include");
                if (idx != -1 && idx + 1 < args.Length) {
                    includeFolder = args[idx + 1];
                }

                idx = Array.FindIndex(args, item => item == "--replace");
                if (idx != -1 && idx + 1 < args.Length) {
                    toReplace = args[idx + 1];
                }

                Console.WriteLine(path);
                Console.WriteLine(outFolder);
                Console.WriteLine(includeFolder);
                Console.WriteLine(toReplace);
                if (path != "nop" && outFolder != "nop" && includeFolder != "nop") {
                    if (toReplace == "nop") toReplace = (new DirectoryInfo(path)).Name;
                    BrawlAICore.Options.IncludePath = includeFolder;
                    ResourceNode node = NodeFactory.FromFile(null, outFolder);
		            ARCEntryNode[] array = FindSomething<ARCEntryNode>(node, ResourceType.AIPD);
                    for (int i = 0; i < array.Length; i++) {
                        if (toReplace == array[i].Parent.Name) {
                            Console.WriteLine("found [" + i + "]: " + toReplace);
                            node.Dispose();
                            List<string> output = BrawlAICore.Compiler.Compile(
                                path,
                                outFolder,
                                i
                            );
                            output.ForEach((string str) => Console.WriteLine(str));
                            break;
                        }
                    }           
                             
                }
            }
        }
    }
}
