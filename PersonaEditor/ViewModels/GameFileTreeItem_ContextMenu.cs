﻿using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using Microsoft.Win32;
using PersonaEditor.Common;
using PersonaEditor.Controls.ToolBox;
using PersonaEditorLib;
using PersonaEditorLib.Text;

namespace PersonaEditor.ViewModels
{
    public partial class GameFileTreeItem : TreeViewItemViewModel
    {
        private void UpdateContextMenu()
        {
            ContextMenu.Clear();

            MenuItem menuItem = null;

            if (PersonaFileHelper.IsEditable(PersonaFile))
            {
                menuItem = new MenuItem();
                menuItem.Header = "View/Edit";
                menuItem.Command = new RelayCommand(ContextMenu_Edit);
                ContextMenu.Add(menuItem);
            }

            menuItem = new MenuItem();
            menuItem.Header = "Replace";
            menuItem.Command = new RelayCommand(ContextMenu_Replace);
            ContextMenu.Add(menuItem);

            ContextMenu.Add(new Separator());

            menuItem = new MenuItem();
            menuItem.Header = "Save As...";
            menuItem.Command = new RelayCommand(ContextMenu_SaveAs);
            ContextMenu.Add(menuItem);

            if (PersonaFileHelper.HaveSubFiles(PersonaFile))
            {
                menuItem = new MenuItem();
                menuItem.Header = "Export All";
                menuItem.Command = new RelayCommand(ContextMenu_SaveAll);
                ContextMenu.Add(menuItem);
            }
        }

        private void ContextMenu_Edit()
        {
            ItemAction?.Invoke(this, UserTreeViewItemEventEnum.Open);
        }

        private void ContextMenu_Replace()
        {
            OpenFileDialog OFD = new OpenFileDialog();
            string name = PersonaFile.Name.Replace('/', '+');
            OFD.Filter = $"RAW(*{Path.GetExtension(name)})|*{Path.GetExtension(name)}";
            OFD.CheckFileExists = true;
            OFD.CheckPathExists = true;
            OFD.Multiselect = false;

            if (PersonaFile.GameData is IImage)
                OFD.Filter += $"|PNG (*.png)|*.png";
            if (PersonaFile.GameData is ITable)
                OFD.Filter += $"|XML data table (*.xml)|*.xml";
            if (PersonaFile.GameData is BMD)
                OFD.Filter += $"|Persona Text Project (*.ptp)|*.ptp";

            if (OFD.ShowDialog() == true)
            {
                if (OFD.FilterIndex == 1)
                {
                    var fileType = PersonaFile.GameData.GetType();
                    var item = GameFormatHelper.TryOpenFile(PersonaFile.Name, File.ReadAllBytes(OFD.FileName), fileType);

                    if (item != null)
                        PersonaFile.GameData = item.GameData;
                }
                else
                {
                    string ext = Path.GetExtension(OFD.FileName);
                    if (ext.Equals(".png", StringComparison.CurrentCultureIgnoreCase))
                        PersonaEditorTools.OpenImageFile(PersonaFile, OFD.FileName);
                    else if (ext.Equals(".xml", StringComparison.CurrentCultureIgnoreCase))
                        PersonaEditorTools.OpenTableFile(PersonaFile, OFD.FileName);
                    else if (ext.Equals(".ptp", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var result = ToolBox.Show(ToolBoxType.OpenPTP);
                        if (result == ToolBoxResult.Ok)
                            PersonaEditorTools.OpenPTPFile(PersonaFile, OFD.FileName, Static.EncodingManager.GetPersonaEncoding(ApplicationSettings.AppSetting.Default.OpenPTP_Font));
                    }
                    else
                        throw new Exception("OpenPersonaFileDialog");
                }

                Update(_personaFile);
                if (IsSelected)
                    ItemAction?.Invoke(this, UserTreeViewItemEventEnum.Selected);
            }
        }

        private void ContextMenu_SaveAs()
        {
            SaveFileDialog SFD = new SaveFileDialog();
            SFD.OverwritePrompt = true;
            SFD.AddExtension = false;
            SFD.FileName = PersonaFile.Name.Replace('/', '+');
            SFD.Filter = $"RAW(*{Path.GetExtension(SFD.FileName)})|*{Path.GetExtension(SFD.FileName)}";

            if (PersonaFile.GameData is IImage)
                SFD.Filter += $"|PNG (*.png)|*.png";
            if (PersonaFile.GameData is ITable)
                SFD.Filter += $"|XML data table (*.xml)|*.xml";
            if (PersonaFile.GameData is BMD)
                SFD.Filter += $"|Persona Text Project (*.ptp)|*.ptp";
            if (PersonaFile.GameData is PTP)
                SFD.Filter += $"|BMD Text File (*.bmd)|*.bmd";

            SFD.InitialDirectory = Path.GetDirectoryName(Static.OpenedFile);
            if (SFD.ShowDialog() == true)
                if (SFD.FilterIndex == 1)
                    File.WriteAllBytes(SFD.FileName, PersonaFile.GameData.GetData());
                else
                {
                    string ext = Path.GetExtension(SFD.FileName);
                    if (ext.Equals(".png", StringComparison.CurrentCultureIgnoreCase))
                        PersonaEditorTools.SaveImageFile(PersonaFile, SFD.FileName);
                    else if (ext.Equals(".ptp", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var result = ToolBox.Show(ToolBoxType.SaveAsPTP);
                        if (result == ToolBoxResult.Ok)
                        {
                            PersonaEncoding temp = ApplicationSettings.AppSetting.Default.SaveAsPTP_CO2N ? Static.EncodingManager.GetPersonaEncoding(ApplicationSettings.AppSetting.Default.SaveAsPTP_Font) : null;
                            PersonaEditorTools.SavePTPFile(PersonaFile, SFD.FileName, temp);
                        }
                    }
                    else if (ext.Equals(".bmd", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Encoding encoding = Static.EncodingManager.GetPersonaEncoding(ApplicationSettings.AppSetting.Default.PTPNewDefault);
                        BMD bmd = new BMD(PersonaFile.GameData as PTP, encoding);
                        File.WriteAllBytes(SFD.FileName, bmd.GetData());
                    }
                    else if (ext.Equals(".xml", StringComparison.CurrentCultureIgnoreCase))
                        PersonaEditorTools.SaveTableFile(PersonaFile, SFD.FileName);
                    else throw new Exception("SavePersonaFileDialog");
                }
        }

        private void ContextMenu_SaveAll()
        {
            System.Windows.Forms.FolderBrowserDialog FBD = new System.Windows.Forms.FolderBrowserDialog();
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = FBD.SelectedPath;

                foreach (var item in PersonaFile.GameData.SubFiles)
                    File.WriteAllBytes(Path.Combine(path, item.Name), item.GameData.GetData());
            }
        }
    }
}