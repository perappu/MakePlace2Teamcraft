using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace MakePlace2Teamcraft
{
    public partial class MainForm : Form
    {
        static List<Dye> dyeList;
        static int currentVersion = 102;
        string strCurrentFile;
        List<Furniture> furnitureList;
        List<Furniture> furnitureDyesList;
        public MainForm()
        {
            dyeList = JsonConvert.DeserializeObject<List<Dye>>(File.ReadAllText("dyeslist.json"));
            InitializeComponent();
        }
        //*** Menu Items ***//
        private void menuItemLoadFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            string rawJSON;
            //Open the itch.io install location if it's there, otherwise just open my documents
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "itch\\apps\\makeplace\\MakePlace\\Save")))
            {
                fileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "itch\\apps\\makeplace\\MakePlace\\Save");
            }
            else
            {
                fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            }
            //show only JSON by default
            fileDialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                strCurrentFile = String.Empty;
                return;
            }
            else
            {
                strCurrentFile = fileDialog.FileName;
                Stream fileStream = fileDialog.OpenFile();
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    rawJSON = streamReader.ReadToEnd();
                }
            }
            LoadMakePlaceData(rawJSON);
        }
        private void menuItemCloseFile_Click(object sender, EventArgs e)
        {
            strCurrentFile = String.Empty;
            txtOutput.Text = string.Empty;
            txtDyes.Text = string.Empty;
            furnitureList = null;
            cboItemList.DataSource = null;
            cboDyes.DataSource = null;
            updItemCount.Value = updItemCount.Minimum;
            updDyeCount.Value = updDyeCount.Minimum;
            btnExportTeamCraft.Enabled = false;
            btnSave.Enabled = false;
            btnSaveDye.Enabled = false;
        }
        private void menuItemExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //*** Form Closing ***//
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Exit?", "Confirm Exit", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }
        //*** File Operations ***//
        private void PopulateForm()
        {
            txtOutput.Text = string.Empty;
            txtDyes.Text = string.Empty;
            foreach (Furniture furniture in furnitureList)
            {
                txtOutput.Text += furniture.ID + " " + furniture.Name + " " + furniture.Count + Environment.NewLine;
            }
            foreach (Furniture dye in furnitureDyesList)
            {
                txtDyes.Text += dye.ID + " " + dye.Name + " " + dye.Count + Environment.NewLine;
            }

            cboItemList.DataSource = furnitureList;
            cboDyes.DataSource = furnitureDyesList;
            btnExportTeamCraft.Enabled = true;
            btnSave.Enabled = true;
            btnSaveDye.Enabled = true;
        }
        /// <summary>
        /// LoadMakePlaceData() parses the raw json from the file into memory and then tells it to refresh the form
        /// </summary>
        /// <param name="rawJSON">string that is chunk of valid json from MakePlace</param>
        private void LoadMakePlaceData(string rawJSON)
        {
            dynamic makePlaceData;
            dynamic allItems;
            furnitureList = new List<Furniture>();
            furnitureDyesList = new List<Furniture>();
            try
            {
                makePlaceData = JObject.Parse(rawJSON);
                allItems = new JArray();
                allItems.Merge(makePlaceData.interiorFixture);
                allItems.Merge(makePlaceData.interiorFurniture);
                allItems.Merge(makePlaceData.exteriorFixture);
                allItems.Merge(makePlaceData.exteriorFurniture);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid file loaded. Please make sure to select valid JSON save file from MakePlace.", "Error");
                return;
            }

            // some older versions of MakePlace store the version number as a string, and some as an int/long
            // we have to explicitly cast it as a string and then back into an int
            if (int.Parse(makePlaceData.metaData.version.Value.ToString()) < currentVersion)
            {
                MessageBox.Show("JSON file is from an outdated version of MakePlace. \nPlease re-open it in the latest version of MakePlace and save it, or use the creator's online tool to update the file.", "Error");
                return;
            }

            Console.WriteLine(allItems);
            foreach (dynamic item in allItems)
            {
                //makeplace seems to use a lot of placeholder items with a value of zero
                if (item.itemId.Value != 0)
                {
                    //check if it has a color first
                    if (item.ContainsKey("properties"))
                    {
                        //we add a "furniture" item for a dye if there's a dye added to the item
                        if (item.properties.ContainsKey("color"))
                        {
                            string color = item.properties.color.Value.ToString();
                            Dye foundDye = dyeList.Find(Dye => Dye.Hex.Contains(color));
                            if (foundDye != null)
                            {
                                if (!furnitureDyesList.Exists(Furniture => Furniture.ID == foundDye.ID))
                                {
                                    furnitureDyesList.Add(new Furniture(
                                        foundDye.Name,
                                        1,
                                        foundDye.ID)
                                        );
                                }
                                else
                                {
                                    furnitureDyesList.Find(x => x.ID == foundDye.ID).Count++;
                                }
                            }
                        }
                        /* TODO: It'd be nice to be able to handle more walls and stuff but right now colors are enough of an ID nightmare
                             if (item.properties.ContainsKey("material"))
                            {
                                string material = item.properties.material.Value.ToString().Split('.')[1];
                            }*/
                    }

                    //if furniture doesn't exist, add a new one
                    if (!furnitureList.Exists(Furniture => Furniture.ID == item.itemId.Value))
                    {
                        furnitureList.Add(new Furniture(
                        item.name.ToString(),
                        1,
                        int.Parse(item.itemId.Value.ToString())
                        ));
                        //otherwise just increase the count on the existing object
                    }
                    else
                    {
                        furnitureList.Find(x => x.ID == item.itemId.Value).Count++;
                    }
                }
            }
            PopulateForm();
        }
        /// <summary>
        /// GenerateTeamCraftLink() creates the link for the teamcraft endpoint and then opens default browser
        /// </summary>
        private void GenerateTeamCraftLink()
        {
            string importString = "";
            foreach (Furniture furniture in furnitureList)
            {
                //teamcraft doesn't like it when there's a ; at the end of the url
                if (importString != "")
                {
                    importString += ";" + furniture.ToImportString();
                }
                else
                {
                    importString += furniture.ToImportString();
                }
            }
            foreach (Furniture dye in furnitureDyesList)
            {
                //teamcraft doesn't like it when there's a ; at the end of the url
                if (importString != "")
                {
                    importString += ";" + dye.ToImportString();
                }
                else
                {
                    importString += dye.ToImportString();
                }
            }
            importString = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(importString));
            //thanks stackoverflow
            Process.Start(new ProcessStartInfo("https://ffxivteamcraft.com/import/" + importString) { UseShellExecute = true });
        }

        //*** Edit Item Methods***//
        private void cboItemList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboItemList.SelectedItem != null)
            {
                updItemCount.Value = ((Furniture)cboItemList.SelectedItem).Count;
            }
        }
        private void cboDyes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboDyes.SelectedItem != null)
            {
                updDyeCount.Value = ((Furniture)cboDyes.SelectedItem).Count;
            }
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            ((Furniture)cboItemList.SelectedItem).Count = (int)updItemCount.Value;
            PopulateForm();
        }
        private void btnSaveDye_Click(object sender, EventArgs e)
        {
            ((Furniture)cboDyes.SelectedItem).Count = (int)updDyeCount.Value;
            PopulateForm();
        }
        private void btnExportTeamCraft_Click(object sender, EventArgs e)
        {
            GenerateTeamCraftLink();
        }
    }
}