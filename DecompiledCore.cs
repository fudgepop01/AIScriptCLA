using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using BrawlLib.SSBB.ResourceNodes;
using BrawlLib.SSBBTypes;
using Compiling;
using Compiling.AIPD;
using Compiling.ATKD;
using Compiling.Main;

namespace BrawlAICore
{
	public static class CustomCompiler
	{
		private static Tuple<AINode, List<string>> BuildAIMain(string[] scripts, string dbgName)
		{
			if (scripts == null || scripts.Length == 0)
			{
				return null;
			}
			AINode aINode = new AINode();
			aINode.FileType = ARCFileType.MiscData;
			aINode.FileIndex = 0;
			aINode.FileId = -1;
			aINode.FileFlags = 512;
			AINode aINode2 = aINode;
			AIGroupNode aIGroupNode = new AIGroupNode();
			aIGroupNode.Name = "Commands";
			AIGroupNode aIGroupNode2 = aIGroupNode;
			AIGroupNode aIGroupNode3 = new AIGroupNode();
			aIGroupNode3.Name = "Strings";
			AIGroupNode aIGroupNode4 = aIGroupNode3;
			aINode2.AddChild(aIGroupNode2);
			aINode2.AddChild(aIGroupNode4);
			List<string> list = new List<string>();
			DebugFile debugFile = new DebugFile();
			MainScanner mainScanner = new MainScanner();
			mainScanner.Init();
			MainParser mainParser = new MainParser(mainScanner);
			mainParser.Init();
			foreach (string path in scripts)
			{
				using FileStream source = new FileStream(path, FileMode.Open);
				mainScanner.Resume();
				mainParser.Resume();
				mainScanner.SetSource(source);
				mainScanner.SrcPath = Path.GetDirectoryName(path);
				if (!mainParser.Parse() || mainScanner.Errors.Count > 0)
				{
					list.Add($"In {Path.GetFileName(path)}");
					list.AddRange(mainScanner.Errors);
					return new Tuple<AINode, List<string>>(null, list);
				}
				AIEntryNode aIEntryNode = new AIEntryNode();
				aIEntryNode.ID = mainParser.Result.ID.ToString("X");
				aIEntryNode.Unknown = mainParser.Result.Unk;
				AIEntryNode aIEntryNode2 = aIEntryNode;
				List<string> list2 = new List<string>();
				byte[] array = (array = mainParser.Result.CompileContent(list2));
				if (list2.Count > 0)
				{
					list.Add($"In {Path.GetFileName(path)}");
					list.AddRange(list2);
					continue;
				}
				List<uint> list3 = new List<uint>();
				for (int j = 0; j < array.Length; j += 4)
				{
					uint item = BitConverter.ToUInt32(new byte[4]
					{
						array[j + 3],
						array[j + 2],
						array[j + 1],
						array[j]
					}, 0);
					list3.Add(item);
				}
				IEnumerable<float> collection = mainParser.Result.Floats.Select((FloatDef x) => Convert.ToSingle(x.Name));
				aIEntryNode2.Globals.AddRange(collection);
				aIEntryNode2.Contents.AddRange(list3);
				aIEntryNode2.Init(aIGroupNode2);
				AIStringNode aIStringNode = new AIStringNode();
				aIStringNode.Strings = mainParser.Result.Strings.ToArray();
				AIStringNode aIStringNode2 = aIStringNode;
				aIStringNode2.Name = ((aIStringNode2.Strings.Length > 0) ? "StringEntry" : "NULL");
				aIGroupNode4.AddChild(aIStringNode2);
				if (mainParser.Breakpoints.Count > 0)
				{
					DebugInformation debugInformation = new DebugInformation();
					debugInformation.File = Path.GetFullPath(path);
					debugInformation.EditDate = File.GetLastWriteTime(path);
					debugInformation.Points = new List<int>();
					debugInformation.Points.AddRange(mainParser.Breakpoints);
					debugInformation.Routine = (ushort)mainParser.Result.ID;
					debugFile.Infos.Add(debugInformation);
				}
			}
			if (debugFile.Infos.Count > 0)
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(DebugFile));
				using FileStream stream = new FileStream($"{dbgName}.xml", FileMode.Create);
				xmlSerializer.Serialize(stream, debugFile);
			}
			return new Tuple<AINode, List<string>>(aINode2, list);
		}

		private static Tuple<AINode, List<string>> BuildAIMain(string srcDir, string dbgName)
		{
			IEnumerable<string> source = from x in Directory.GetFiles(srcDir)
				where x.EndsWith(".as")
				select x;
			if (source.Count() <= 0)
			{
				return new Tuple<AINode, List<string>>(null, new List<string>());
			}
			return BuildAIMain(source.ToArray(), dbgName);
		}

		private static Tuple<ARCEntryNode, List<string>> BuildAIPD(string source)
		{
			StringWriter error = new StringWriter();
			Console.SetError(error);
			List<string> list = new List<string>();
			using FileStream file = new FileStream(source, FileMode.Open);
			AIPDScanner aIPDScanner = new AIPDScanner(file);
			aIPDScanner.SrcPath = Path.GetDirectoryName(source);
			AIPDScanner aIPDScanner2 = aIPDScanner;
			AIPDParser aIPDParser = new AIPDParser(aIPDScanner2);
			if (!aIPDParser.Parse() || aIPDScanner2.Errors.Count > 0)
			{
				list.Add($"In {Path.GetFileName(source)}");
				list.AddRange(aIPDScanner2.Errors);
				return new Tuple<ARCEntryNode, List<string>>(null, list);
			}
			string tempFileName = Path.GetTempFileName();
			List<string> list2 = new List<string>();
			File.WriteAllBytes(tempFileName, aIPDParser.Result.Compile(list2));
			if (list2.Count > 0)
			{
				list.Add($"In {Path.GetFileName(source)}");
				list.AddRange(list2);
				return new Tuple<ARCEntryNode, List<string>>(null, list);
			}
			ARCEntryNode aRCEntryNode = new ARCEntryNode();
			aRCEntryNode.FileType = ARCFileType.MiscData;
			aRCEntryNode.FileIndex = 0;
			aRCEntryNode.FileId = -1;
			aRCEntryNode.FileFlags = 256;
			ARCEntryNode aRCEntryNode2 = aRCEntryNode;
			aRCEntryNode2.Replace(tempFileName);
			return new Tuple<ARCEntryNode, List<string>>(aRCEntryNode2, list);
		}

		private static Tuple<ARCEntryNode, List<string>> BuildAIPDFromDir(string dir)
		{
			IEnumerable<string> source = from x in Directory.GetFiles(dir)
				where x.EndsWith(".aipd")
				select x;
			if (source.Count() != 1)
			{
				return new Tuple<ARCEntryNode, List<string>>(null, new List<string>());
			}
			return BuildAIPD(source.First());
		}

		private static Tuple<ARCEntryNode, List<string>> BuildATKD(string source)
		{
			List<string> list = new List<string>();
			using FileStream file = new FileStream(source, FileMode.Open);
			ATKDScanner aTKDScanner = new ATKDScanner(file);
			aTKDScanner.SrcPath = Path.GetDirectoryName(source);
			ATKDScanner aTKDScanner2 = aTKDScanner;
			ATKDParser aTKDParser = new ATKDParser(aTKDScanner2);
			if (!aTKDParser.Parse() || aTKDScanner2.Errors.Count > 0)
			{
				list.Add($"In {Path.GetFileName(source)}");
				list.AddRange(aTKDScanner2.Errors);
				return new Tuple<ARCEntryNode, List<string>>(null, list);
			}
			string tempFileName = Path.GetTempFileName();
			List<string> list2 = new List<string>();
			File.WriteAllBytes(tempFileName, aTKDParser.Result.Compile(list2));
			if (list2.Count > 0)
			{
				list.Add($"In {Path.GetFileName(source)}");
				list.AddRange(list2);
				return new Tuple<ARCEntryNode, List<string>>(null, list);
			}
			ARCEntryNode aRCEntryNode = new ARCEntryNode();
			aRCEntryNode.FileType = ARCFileType.MiscData;
			aRCEntryNode.FileIndex = 0;
			aRCEntryNode.FileId = -1;
			aRCEntryNode.FileFlags = 768;
			ARCEntryNode aRCEntryNode2 = aRCEntryNode;
			aRCEntryNode2.Replace(tempFileName);
			return new Tuple<ARCEntryNode, List<string>>(aRCEntryNode2, list);
		}

		private static Tuple<ARCEntryNode, List<string>> BuildATKDFromDir(string dir)
		{
			IEnumerable<string> source = from x in Directory.GetFiles(dir)
				where x.EndsWith(".atkd")
				select x;
			if (source.Count() != 1)
			{
				return new Tuple<ARCEntryNode, List<string>>(null, new List<string>());
			}
			return BuildATKD(source.First());
		}

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

		private static void PACExport(string place, ResourceNode node)
		{
			string tempFileName = Path.GetTempFileName();
			node.Export(tempFileName);
			node.Dispose();
			if (File.Exists(place))
			{
				File.Delete(place);
			}
			File.Move(tempFileName, place);
			if (File.Exists("temp_AIPD"))
			{
				File.Delete("temp_AIPD");
			}
			if (File.Exists("temp_ATKD"))
			{
				File.Delete("temp_ATKD");
			}
		}

		private static void ReplaceNode<T>(ResourceNode target, T replacer, ResourceType t) where T : ResourceNode
		{
			bool flag = false;
			for (int i = 0; i < target.Children.Count; i++)
			{
				if (flag = target.Children[i].ResourceType == t)
				{
					target.Children[i].Remove();
					if (replacer != null)
					{
						target.Children.Insert(i, replacer);
					}
					break;
				}
			}
			if (!flag && replacer != null)
			{
				target.AddChild(replacer);
			}
		}

		private static ARCNode NodeSelector(ResourceNode node, int selector = 0)
		{
			AIPDNode[] array = FindSomething<AIPDNode>(node, ResourceType.AIPD);
			if (array.Length > 1)
			{
				return array.ElementAt(selector).Parent as ARCNode;
			}
			return array.First().Parent as ARCNode;
		}

		public static List<string> Compile(string srcDir, string targetPac, int selector = 0, bool isCommon = false)
		{
			try
			{
				string dbgName = Path.GetDirectoryName(targetPac) + "\\" + Path.GetFileNameWithoutExtension(targetPac);
				new StringWriter();
				List<string> list = new List<string>();
				Tuple<AINode, List<string>> tuple = BuildAIMain(srcDir, dbgName);
				Tuple<ARCEntryNode, List<string>> tuple2 = BuildAIPDFromDir(srcDir);
				Tuple<ARCEntryNode, List<string>> tuple3 = BuildATKDFromDir(srcDir);
				AINode item = tuple.Item1;
				ARCEntryNode item2 = tuple2.Item1;
				ARCEntryNode item3 = tuple3.Item1;
				ResourceNode node = NodeFactory.FromFile(null, targetPac);
				ARCNode target = NodeSelector(node, selector);
				if (list.Count == 0)
				{
					ReplaceNode(target, item, ResourceType.AI);
					ReplaceNode(target, item2, ResourceType.AIPD);
					ReplaceNode(target, item3, ResourceType.ATKD);
          // CUSTOM STUFF HERE
          Console.WriteLine(isCommon);
          if (isCommon) {
            ResourceNode AINode = target.Children.Where((ResourceNode x) => x.ResourceType == ResourceType.AI).First();
            Console.WriteLine(AINode.Name);
            ResourceNode[] stringNodes = AINode.FindChildrenByType("", ResourceType.AIString);

            for (int i = 0; i < stringNodes.Length; i++) {
              Console.WriteLine("entry: " + i);

              AIStringNode stringNode = (AIStringNode) stringNodes[i];
              if (stringNode.Strings.Length > 0 && stringNode.Strings[0].Contains("PADDING_FILE")) {
                Console.WriteLine("Found");
                stringNode.Strings = new string[] { "PADDING_FILE", new string('\xFF', ((32704 - 32) - target.WorkingUncompressed.Length) / 2) };
                for (int j = 0; j < stringNode.Strings.Length; j++) {
                  Console.WriteLine(stringNode.Strings[j]);
                }
                ReplaceNode(AINode, stringNode, ResourceType.AIString);
                break;
              }
            }
            // ReplaceNode(item, AINode, ResourceType.AIString);
          }

					PACExport(targetPac, node);
				}
				tuple.Item2.AddRange(tuple2.Item2);
				tuple.Item2.AddRange(tuple3.Item2);
				return tuple.Item2;
			}
			catch (Exception ex)
			{
				List<string> list2 = new List<string>();
				list2.Add("Unexpected error occurred while compiling");
				list2.Add(ex.Message);
				return list2;
			}
		}

		public static List<string> CompileAIMain(string srcDir, string targetPac, int selector = 0)
		{
			try
			{
				string dbgName = Path.GetDirectoryName(targetPac) + "\\" + Path.GetFileNameWithoutExtension(targetPac);
				Tuple<AINode, List<string>> tuple = BuildAIMain(srcDir, dbgName);
				AINode item = tuple.Item1;
				ResourceNode node = NodeFactory.FromFile(null, targetPac);
				ARCNode target = NodeSelector(node, selector);
				if (item != null)
				{
					ReplaceNode(target, item, ResourceType.AI);
				}
				PACExport(targetPac, node);
				return tuple.Item2;
			}
			catch (Exception ex)
			{
				List<string> list = new List<string>();
				list.Add("Unexpected error occurred while compiling AIMain");
				list.Add(ex.Message);
				return list;
			}
		}

		public static List<string> CompileAIPD(string srcDir, string targetPac, int selector = 0)
		{
			try
			{
				Tuple<ARCEntryNode, List<string>> tuple = BuildAIPDFromDir(srcDir);
				ARCEntryNode item = tuple.Item1;
				ResourceNode node = NodeFactory.FromFile(null, targetPac);
				ARCNode target = NodeSelector(node, selector);
				if (item != null)
				{
					ReplaceNode(target, item, ResourceType.AIPD);
				}
				PACExport(targetPac, node);
				return tuple.Item2;
			}
			catch (Exception ex)
			{
				List<string> list = new List<string>();
				list.Add("Unexpected error occurred while compiling AIPD");
				list.Add(ex.Message);
				return list;
			}
		}

		public static List<string> CompileATKD(string srcDir, string targetPac, int selector = 0)
		{
			try
			{
				Tuple<ARCEntryNode, List<string>> tuple = BuildATKDFromDir(srcDir);
				ARCEntryNode item = tuple.Item1;
				ResourceNode node = NodeFactory.FromFile(null, targetPac);
				ARCNode target = NodeSelector(node, selector);
				if (item != null)
				{
					ReplaceNode(target, item, ResourceType.ATKD);
				}
				PACExport(targetPac, node);
				return tuple.Item2;
			}
			catch (Exception ex)
			{
				List<string> list = new List<string>();
				list.Add("Unexpected error occurred while compiling ATKD");
				list.Add(ex.Message);
				return list;
			}
		}
	}
}
