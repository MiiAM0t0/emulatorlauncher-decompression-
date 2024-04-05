﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EmulatorLauncher.Common;
using EmulatorLauncher.Common.FileFormats;

namespace EmulatorLauncher
{
    partial class PortsLauncherGenerator : Generator
    {
        private void ConfigurePort(List<string> commandArray, string rom, string exe)
        {
            ConfigureSonic3air(commandArray, rom, exe);
            ConfigureSonicMania(commandArray, rom, exe);
        }

        #region ports
        private void ConfigureSonic3air(List<string> commandArray, string rom, string exe)
        {
            if (_emulator != "sonic3air")
                return;

            string configFolder = Path.Combine(_path, "savedata");
            if (!Directory.Exists(configFolder))
                try { Directory.CreateDirectory(configFolder); } catch { }

            // Settings file
            string settingsFile = Path.Combine(configFolder, "settings.json");

            var settings = DynamicJson.Load(settingsFile);

            settings["AutoAssignGamepadPlayerIndex"] = "-1";
            settings["GameExePath"] = exe.Replace("\\", "\\\\");
            settings["Fullscreen"] = _fullscreen ? "1" : "0";
            settings["RomPath"] = rom.Replace("\\", "/");
            BindFeature(settings, "ControllerRumblePlayer1", "sonic3_rumble", "0.0");
            BindFeature(settings, "ControllerRumblePlayer2", "sonic3_rumble", "0.0");

            if (SystemConfig.isOptSet("MonitorIndex") && !string.IsNullOrEmpty(SystemConfig["MonitorIndex"]))
            {
                settings["DisplayIndex"] = (SystemConfig["MonitorIndex"].ToInteger() - 1).ToString();
            }
            else
                settings["DisplayIndex"] = "0";

            BindFeature(settings, "RenderMethod", "sonic3_renderer", "opengl-full");
            BindFeature(settings, "FrameSync", "sonic3_vsync", "1");
            BindBoolFeature(settings, "Upscaling", "integerscale", "1", "0");
            BindFeature(settings, "Filtering", "sonic3_shader", "0");
            BindBoolFeature(settings, "PerformanceDisplay", "sonic3_showfps", "1", "0");
            BindFeature(settings, "Scanlines", "sonic3_scanlines", "0");

            ConfigureSonic3airControls(configFolder, settings);

            settings.Save();

            string configFile = Path.Combine(_path, "config.json");

            // Config file
            var config = DynamicJson.Load(configFile);

            var devmode = config.GetOrCreateContainer("DevMode");
            devmode["SkipExitConfirmation"] = "1";

            config.Save();
        }

        private void ConfigureSonicMania(List<string> commandArray, string rom, string exe)
        {
            if (_emulator != "sonicmania")
                return;

            string sourcePath = _path;
            string romPath = Path.GetDirectoryName(rom);

            // Put the 2 emulator files in the rom folder (check versions)
            string sourceGameExe = Path.Combine(_path, _exeName);
            string targetGameExe = Path.Combine(romPath, _exeName);
            string sourceDLL = Path.Combine(_path, "game.dll");
            string targetDLL = Path.Combine(romPath, "game.dll");

            if (!File.Exists(targetGameExe) || !File.Exists(targetDLL))
            {
                try
                {
                    File.Copy(sourceGameExe, targetGameExe, true);
                    File.Copy(sourceDLL, targetDLL, true);
                }
                catch { }
            }

            // check versions
            if (File.Exists(targetGameExe))
            {
                var sourceVersionInfo = FileVersionInfo.GetVersionInfo(sourceGameExe);
                var targetVersionInfo = FileVersionInfo.GetVersionInfo(targetGameExe);
                string sourceVersion = sourceVersionInfo.FileMajorPart + "." + sourceVersionInfo.FileMinorPart + "." + sourceVersionInfo.FileBuildPart + "." + sourceVersionInfo.FilePrivatePart;
                string targetVersion = targetVersionInfo.FileMajorPart + "." + targetVersionInfo.FileMinorPart + "." + targetVersionInfo.FileBuildPart + "." + targetVersionInfo.FilePrivatePart;

                if (sourceVersion != targetVersion)
                {
                    try
                    {
                        File.Copy(sourceGameExe, targetGameExe, true);
                        File.Copy(sourceDLL, targetDLL, true);
                    }
                    catch { }
                }
            }
            _path = romPath;
            exe = targetGameExe;

            // Settings
            string settingsFile = Path.Combine(romPath, "Settings.ini");

            using (var ini = IniFile.FromFile(settingsFile))
            {
                ini.WriteValue("Video", "exclusiveFS", "n");
                ini.WriteValue("Video", "border", "n");
                BindBoolIniFeature(ini, "Video", "vsync", "sonicmania_vsync", "n", "y");
                BindBoolIniFeature(ini, "Video", "tripleBuffering", "sonicmania_triple_buffering", "y", "n");
                BindIniFeature(ini, "Game", "language", "sonicmania_lang", "0");

                if (_fullscreen)
                    ini.WriteValue("Video", "windowed", "n");
                else
                {
                    ini.WriteValue("Video", "windowed", "y");

                    if (_resolution != null)
                    {
                        ini.WriteValue("Video", "winWidth", _resolution.Width.ToString());
                        ini.WriteValue("Video", "winHeight", _resolution.Height.ToString());
                        ini.WriteValue("Video", "refreshRate", _resolution.DisplayFrequency.ToString());
                    }
                    else
                    {
                        var res = ScreenResolution.CurrentResolution;
                        ini.WriteValue("Video", "winWidth", res.Width.ToString());
                        ini.WriteValue("Video", "winHeight", res.Height.ToString());
                        ini.WriteValue("Video", "winHeight", res.DisplayFrequency.ToString());
                    }
                }

                if (SystemConfig.isOptSet("sonicmania_shader") && !string.IsNullOrEmpty(SystemConfig["sonicmania_shader"]) && SystemConfig["sonicmania_shader"] != "none")
                {
                    ini.WriteValue("Video", "shaderSupport", "y");
                    ini.WriteValue("Video", "screenShader", SystemConfig["sonicmania_shader"]);
                }
                else
                {
                    ini.WriteValue("Video", "shaderSupport", "n");
                    ini.WriteValue("Video", "screenShader", "0");
                }

                ConfigureSonicManiaControls(ini);

                ini.Save();
            }
        }
        #endregion
    }
}
