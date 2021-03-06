﻿using System.IO;
using System.Text;
using LibHac;
using static hactoolnet.Print;

namespace hactoolnet
{
    internal static class ProcessPackage
    {
        public static void ProcessPk11(Context ctx)
        {
            using (var file = new FileStream(ctx.Options.InFile, FileMode.Open, FileAccess.Read))
            {
                var package1 = new Package1(ctx.Keyset, file);
                string outDir = ctx.Options.OutDir;

                if (outDir != null)
                {
                    Directory.CreateDirectory(outDir);

                    package1.Pk11.OpenWarmboot().WriteAllBytes(Path.Combine(outDir, "Warmboot.bin"), ctx.Logger);
                    package1.Pk11.OpenNxBootloader().WriteAllBytes(Path.Combine(outDir, "NX_Bootloader.bin"), ctx.Logger);
                    package1.Pk11.OpenSecureMonitor().WriteAllBytes(Path.Combine(outDir, "Secure_Monitor.bin"), ctx.Logger);

                    using (var decFile = new FileStream(Path.Combine(outDir, "Decrypted.bin"), FileMode.Create))
                    {
                        package1.OpenPackage1Ldr().CopyTo(decFile);
                        package1.Pk11.OpenDecryptedPk11().CopyTo(decFile);
                    }
                }
            }
        }

        public static void ProcessPk21(Context ctx)
        {
            using (var file = new FileStream(ctx.Options.InFile, FileMode.Open, FileAccess.Read))
            {
                var package2 = new Package2(ctx.Keyset, file);

                ctx.Logger.LogMessage(package2.Print());

                string outDir = ctx.Options.OutDir;

                if (outDir != null)
                {
                    Directory.CreateDirectory(outDir);

                    package2.OpenKernel().WriteAllBytes(Path.Combine(outDir, "Kernel.bin"), ctx.Logger);
                    package2.OpenIni1().WriteAllBytes(Path.Combine(outDir, "INI1.bin"), ctx.Logger);

                    using (var decFile = new FileStream(Path.Combine(outDir, "Decrypted.bin"), FileMode.Create))
                    {
                        package2.OpenHeaderPart1().CopyTo(decFile);
                        package2.OpenHeaderPart2().CopyTo(decFile);
                        package2.OpenKernel().CopyTo(decFile);
                        package2.OpenIni1().CopyTo(decFile);
                    }
                }
            }
        }

        private static readonly string[] Package2SectionNames = { "Kernel", "INI1", "Empty" };

        private static string Print(this Package2 package2)
        {
            int colLen = 36;
            var sb = new StringBuilder();
            sb.AppendLine();

            sb.AppendLine("PK21:");
            PrintItem(sb, colLen, "Signature:", package2.Header.Signature);
            PrintItem(sb, colLen, "Header Version:", $"{package2.HeaderVersion:x2}");

            for (int i = 0; i < 3; i++)
            {
                sb.AppendLine($"Section {i} ({Package2SectionNames[i]}):");

                PrintItem(sb, colLen, "    Hash:", package2.Header.SectionHashes[i]);
                PrintItem(sb, colLen, "    CTR:", package2.Header.SectionCounters[i]);
                PrintItem(sb, colLen, "    Load Address:", $"{package2.Header.SectionOffsets[i] + 0x80000000:x8}");
                PrintItem(sb, colLen, "    Size:", $"{package2.Header.SectionSizes[i]:x8}");
            }

            return sb.ToString();
        }
    }
}
