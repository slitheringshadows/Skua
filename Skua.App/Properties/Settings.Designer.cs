﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Skua.App.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.2.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <string>Tercessuinotlim,tercessuinotlim,Enter,Spawn</string>
  <string>Nulgath,tercessuinotlim,Boss2,Right</string>
  <string>VHL &amp; Taro,tercessuinotlim,Taro,Left</string>
  <string>Swindle,tercessuinotlim,Swindle,Left</string>
  <string>Polish,tercessuinotlim,m12,Right</string>
  <string>Yin &amp; Yang,tercessuinotlim,Twins,Left</string>
  <string>Carnage,tercessuinotlim,m4,Right</string>
  <string>Lae,tercessuinotlim,m5,Top</string>
  <string>Binky,doomvault,r5,Left</string>
  <string>Museum,museum,Crossroad,Left</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection FastTravels {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["FastTravels"]));
            }
            set {
                this["FastTravels"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection DefaultOptions {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["DefaultOptions"]));
            }
        }
    }
}
