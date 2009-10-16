﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.ComponentModel;
using System.Windows.Data;

using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Gui;

namespace ICSharpCode.AvalonEdit.AddIn.Options
{
	[Serializable]
	public class CodeEditorOptions : TextEditorOptions
	{
		public static CodeEditorOptions Instance {
			get { return PropertyService.Get("CodeEditorOptions", new CodeEditorOptions()); }
		}
		
		// always support scrolling below the end of the document - it's better when folding is enabled
		[DefaultValue(true)]
		public override bool AllowScrollBelowDocument {
			get { return true; }
			set {
				if (value == false)
					throw new NotSupportedException();
			}
		}
		
		string fontFamily = Core.WinForms.WinFormsResourceService.DefaultMonospacedFont.Name;
		
		public string FontFamily {
			get { return fontFamily; }
			set {
				if (fontFamily != value) {
					fontFamily = value;
					OnPropertyChanged("FontFamily");
				}
			}
		}
		
		double fontSize = 13.0;
		
		[DefaultValue(13.0)]
		public double FontSize {
			get { return fontSize; }
			set {
				if (fontSize != value) {
					fontSize = value;
					OnPropertyChanged("FontSize");
				}
			}
		}
		
		bool showLineNumbers = true;
		
		[DefaultValue(true)]
		public bool ShowLineNumbers {
			get { return showLineNumbers; }
			set {
				if (showLineNumbers != value) {
					showLineNumbers = value;
					OnPropertyChanged("ShowLineNumbers");
				}
			}
		}
		
		bool wordWrap;
		
		[DefaultValue(false)]
		public bool WordWrap {
			get { return wordWrap; }
			set {
				if (wordWrap != value) {
					wordWrap = value;
					OnPropertyChanged("WordWrap");
				}
			}
		}
		
		bool ctrlClickGoToDefinition = true;
		
		[DefaultValue(true)]
		public bool CtrlClickGoToDefinition {
			get { return ctrlClickGoToDefinition; }
			set {
				if (ctrlClickGoToDefinition != value) {
					ctrlClickGoToDefinition = value;
					OnPropertyChanged("CtrlClickGoToDefinition");
				}
			}
		}
		
		public void BindToTextEditor(TextEditor editor)
		{
			editor.Options = this;
			editor.SetBinding(TextEditor.FontFamilyProperty, new Binding("FontFamily") { Source = this });
			editor.SetBinding(TextEditor.FontSizeProperty, new Binding("FontSize") { Source = this });
			editor.SetBinding(TextEditor.ShowLineNumbersProperty, new Binding("ShowLineNumbers") { Source = this });
			editor.SetBinding(TextEditor.WordWrapProperty, new Binding("WordWrap") { Source = this });
		}
	}
}
