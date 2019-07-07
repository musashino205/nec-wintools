using System;
using System.IO;
using System.Text;
using System.Threading;

namespace nec_wintools
{
	class Program
	{
		public struct Properties
		{
			public bool debug;
			public string inFile;
			public string outFile;
			public string key;
		}

		static int XorPattern(ref byte[] data, int len, ref string key, int k_len, int k_off)
		{
			int data_pos = 0;
			int offset = k_off;
			byte[] byteKey = Encoding.UTF8.GetBytes(key);

			while (len-- > 0)
			{
				data[data_pos] ^= byteKey[offset];
				data_pos++;
				offset = (offset + 1) % k_len;
			}

			return offset;
		}

		static void XorData(ref byte[] data, int len, ref byte[] pattern)
		{
			int data_pos = 0;

			for (int i = 0; i < len; i++)
			{
				data[data_pos] ^= pattern[i];
				data_pos++;
			}
		}

		static int Do_FwEncode(Properties props)
		{
			int max_key_len = 16;
			int pattern_len = 251;
			int read_len;
			int ptn = 1;
			int k_off = 0;
			byte[] buf_pattern = new byte[4096];
			byte[] buf = new byte[4096];

			int k_len = props.key.Length;
			if (k_len == 0 || k_len > max_key_len)
			{
				Console.WriteLine("Key length is not in range (0, {0})", max_key_len);
				return 1;
			}

			if (!File.Exists(props.inFile))
			{
				Console.WriteLine("cannot open input file (not found)");
				return 1;
			}

			FileStream inFs = null;
			FileStream outFs = null;
			FileStream patFs = null;
			FileStream xpatFs = null;

			try
			{
				inFs = new FileStream(props.inFile, FileMode.Open, FileAccess.Read);
				outFs = new FileStream(props.outFile, FileMode.OpenOrCreate, FileAccess.Write);

				if (props.debug)
				{
					patFs = new FileStream(@"pattern.bin", FileMode.OpenOrCreate, FileAccess.Write);
					xpatFs = new FileStream(@"pattern.xor", FileMode.OpenOrCreate, FileAccess.Write);
				}
			}
			catch (IOException i)
			{
				Console.WriteLine(i.Message);
				return 1;
			}

			while ((read_len = inFs.Read(buf, 0, buf.Length)) > 0)
			{
				for (int i = 0; i < read_len; i++)
				{
					buf_pattern[i] = Convert.ToByte(ptn);
					ptn++;

					if (ptn > pattern_len)
						ptn = 1;
				}

				if (props.debug)
					patFs.Write(buf_pattern, 0, read_len);

				k_off = XorPattern(ref buf_pattern, read_len, ref props.key, k_len, k_off);
				if (props.debug)
					xpatFs.Write(buf_pattern, 0, read_len);

				XorData(ref buf, read_len, ref buf_pattern);

				outFs.Write(buf, 0, read_len);
			}

			inFs.Close();
			outFs.Close();
			if (props.debug)
			{
				patFs.Close();
				xpatFs.Close();
			}

			return 0;
		}

		static int Main(string[] args)
		{
			int ret;
			Properties props = new Properties();

			if (args.Length < 1)
			{
				Console.WriteLine("error: no command-line arguments");
				return 1;
			}

			ArgMap argMap = new ArgMap();
			argMap.Init_args(args, ref props);

			if (props.debug)
			{
				Console.WriteLine("\n==== args ====");
				foreach (string arg in args)
				{
					Console.WriteLine(arg);
				}
				Console.WriteLine("=============\n");
			}

			if (props.inFile == null || props.outFile == null)
			{
				Console.WriteLine("error: parameter error, exit");
				if (props.debug)
					Thread.Sleep(4000);
				return 1;
			}

			Console.WriteLine("********** Info **********\n" +
				" input file:\t{0}\n\t\t({1})\n\n" +
				" output file:\t{2}\n\t\t({3})\n\n" +
				" key:\t\t{4}\n",
				Path.GetFileName(props.inFile), Directory.GetParent(props.inFile),
				Path.GetFileName(props.outFile), Directory.GetParent(props.outFile),
				props.key);

			switch (args[0])
			{
				case "enc":
					Console.WriteLine("=== encode/decode mode ===");
					if (props.key == null)
					{
						Console.WriteLine("error: \"key\" is not specified");
						ret = 1;
						break;
					}
					ret = Do_FwEncode(props);
					break;
				default:
					Console.WriteLine("error: mode is missing");
					ret = 1;
					break;
			}

			if (ret != 0)
			{
				Console.WriteLine("ERROR");
				if (props.debug)
					Thread.Sleep(4000);
				return ret;
			}

			Console.WriteLine("DONE");

			if (props.debug)
				Thread.Sleep(4000);
			return ret;
		}
	}
}
