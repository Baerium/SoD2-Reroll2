using System;
using System.IO;
using WindowsInput;
using Patagames.Ocr;
using System.Timers;
using System.Drawing;
using WindowsInput.Native;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SoD2_Reroll
{
    public partial class SoD2Reroll : Form
    {
        private System.Timers.Timer rerollTimer;
        private readonly InputSimulator sim = new InputSimulator();
        private bool reroll = false, firstIteration = false;
        private short wait = 10000, survivor = 1;
        private readonly short interval = 50;
        private Size resolution = new Size(1920, 1080);
        private static readonly int heightTrait = 125, heightSkill = 35;
        StreamWriter sw;

        //Array that holds the currently selected skills and traits in combo boxes
        private string[] activeSkills = { "", "", "" };
        private string[,] activeTraits = { { "", "", "" }, { "", "", "" }, { "", "", "" } };

        //Array that shows which set of skills and traits still need to be found
        private int[] activeSurvivors = { 1, 1, 1};
        //private int[] activeSurvivors = { 1, 2, 3 };

        public SoD2Reroll() 
        { 
            InitializeComponent(); 
        }

        private void SoD2Reroll_Load(object sender, EventArgs e)
        {
            //Arrays of all skills and traits obtainable by random characters, excludes red talon and heartland exlusives
            //string[] skills = Properties.Resources.skills.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            //string[] traits = Properties.Resources.traits.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // TODO:
            // fatal flaw: text document has "LF" as sentence-ending! will be bad on mac

            // use "\n" instead of environment.newline - since in windows a newline is \r\n
            string[] skills = Properties.Resources.skills.Split(new[] { "\n" }, StringSplitOptions.None);
            string[] traits = Properties.Resources.traits.Split(new[] { "\n" }, StringSplitOptions.None);

            //Create output text file or delete contents if it already exists
            File.WriteAllText(Directory.GetCurrentDirectory() + "\\output.txt", String.Empty);
            sw = new StreamWriter(Directory.GetCurrentDirectory() + "\\output.txt");

            cbSurvivor1.Items.AddRange(skills);
            cbSurvivor2.Items.AddRange(skills);
            cbSurvivor3.Items.AddRange(skills);

            cbS1Trait1.Items.AddRange(traits);
            cbS1Trait2.Items.AddRange(traits);
            cbS1Trait3.Items.AddRange(traits);
            cbS2Trait1.Items.AddRange(traits);
            cbS2Trait2.Items.AddRange(traits);
            cbS2Trait3.Items.AddRange(traits);
            cbS3Trait1.Items.AddRange(traits);
            cbS3Trait2.Items.AddRange(traits);
            cbS3Trait3.Items.AddRange(traits);

            cbResolution.Items.AddRange(new string[] { "1280x720", "1360x720", "1366x720", "1600x900", "1920x1080", "2560x1440" });
            cbResolution.SelectedIndex = 4;

            rerollTimer = new System.Timers.Timer(wait);
            rerollTimer.Elapsed += RerollTimer_Elapsed;

            Stop();
        }

        private void RerollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Disable the timer to stop it from running again before an iteration is done
            rerollTimer.Enabled = false;
            rerollTimer.Interval = interval;

            //Disable reroll if control is held
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                Stop();
            }

            if (firstIteration)
            {
                //Move cursor to the first button
                sim.Keyboard.KeyPress(VirtualKeyCode.UP);
                sim.Keyboard.KeyPress(VirtualKeyCode.LEFT);
                sim.Keyboard.KeyPress(VirtualKeyCode.LEFT);

                firstIteration = false;
            }

            //define variables for screenshot position
            int leftTrait = 0, leftSkill = 0;
            int topTrait = (int)Math.Round(resolution.Height / 3.65), topSkill = (int)Math.Round(resolution.Height / 1.55);

            //Sets left and top vaiables for each survivor based on resoltuion
            switch (survivor)
            {
                case 1:
                    leftTrait = (int)Math.Round(resolution.Width / 5.47);
                    leftSkill = resolution.Width / 5;
                    break;
                case 2:
                    leftTrait = (int)Math.Round(resolution.Width / 1.995);
                    leftSkill = (int)Math.Round(resolution.Width / 1.925);
                    break;
                case 3:
                    leftTrait = (int)Math.Round(4.1 * resolution.Width / 5);
                    leftSkill = (int)Math.Round(4.2 * resolution.Width / 5);
                    break;
            }

            //Take screenshots for OCR to read
            Bitmap traitsImg = Screenshot(leftTrait, topTrait, heightTrait);
            Bitmap skillsImg = Screenshot(leftSkill, topSkill, heightSkill);

            traitsImg.Save(Directory.GetCurrentDirectory() + "\\TestImages\\SoD2TraitScreenshot.jpg");
            skillsImg.Save(Directory.GetCurrentDirectory() + "\\TestImages\\SoD2SkillScreenshot.jpg");

            //If a set of triats / skills have not been found already check if they are in the current screenshot

            //sw.WriteLine("Current survivor: " + survivor);

            if ( (activeSurvivors[survivor-1] == 1) && TextMatch(leftTrait, topTrait, leftSkill, topSkill, traitsImg, skillsImg, survivor))
            {
                activeSurvivors[survivor-1] = 0;
                survivor++;
                sim.Keyboard.KeyPress(VirtualKeyCode.RIGHT);
                EnableTimer();
            }
            else
            {
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                EnableTimer();
            }


            traitsImg.Dispose();
            skillsImg.Dispose();

            if (survivor > 3)
            {
                Stop();
            }
        }

        private int getCharactersInTraits(int survivorNumber)
        {
            int result = 0;
            result += activeTraits[survivorNumber - 1, 0].Length;
            result += activeTraits[survivorNumber - 1, 1].Length;
            result += activeTraits[survivorNumber - 1, 2].Length;
            return result;
        }

        private int getCharactersInSkills(int survivorNumber)
        {
            int result = 0;
            result += activeSkills[survivorNumber - 1].Length;

            return result;
        }

        private Bitmap Screenshot(int left, int top, int height)
        {
            Bitmap img = new Bitmap(375, height);
            Graphics g = Graphics.FromImage(img);
            g.CopyFromScreen(left, top, 0, 0, new Size(375, height), CopyPixelOperation.SourceCopy);
            return img;
        }


        // New version 
        // more code - but less computation since 'trivial' cases are not check with an O(n*m) algorithm
        private bool TextMatch(int leftTrait, int topTrait, int leftSkill, int topSkill, Bitmap traitsOrg, Bitmap skills, int survivorNumber)
        {
            Boolean match = false;

            Bitmap traits = ApplyTresholdAndInvert(traitsOrg, 100, "testtrait" + leftTrait.ToString());
            //Bitmap skills = ApplyTresholdAndInvert(skillsOrg, 100, "testskill" + leftSkill.ToString());

            // The amount of characters that have to be changed to get a match
            int stringDistanceTreshold = 3;

            using (var objOcr = OcrApi.Create())
            {
                objOcr.Init(Patagames.Ocr.Enums.Languages.English);
                string plainTextTraits = objOcr.GetTextFromImage(traits).ToUpper();
                string plainTextSkills = objOcr.GetTextFromImage(skills);

                List<String> traitsList = new List<String>(plainTextTraits.Split(
                            new string[] { "\r\n", "\r", "\n" },
                            StringSplitOptions.None
                            ));

                // remove spaces
                for(int i=0; i<traitsList.Count; i++)
                {
                    traitsList[i] = Regex.Replace(traitsList[i], @"\s+", "");
                }
                // delete newline and empty entries
                traitsList.RemoveAll(s => s.Equals("\n") || s.Equals("\r\n") || s.Equals("\r") || s.Length == 0);

                //string formattedTextTraits = Regex.Replace(plainTextTraits, @"\s+", "").ToUpper();
                string formattedTextSkills = Regex.Replace(plainTextSkills, @"\s+", "").ToUpper();

                // create check for every trait found - default value is false
                Boolean[] traitsChecked = new Boolean[traitsList.Count];

                // check every game survivor trait if its contained in the searched ones
                for(int i=0; i<traitsList.Count; i++ )
                {
                    // if all activetraits are empty - just set to true and end this loop
                    if (activeTraits[survivorNumber - 1, 0].Length == 0 &&
                        activeTraits[survivorNumber - 1, 1].Length == 0 &&
                        activeTraits[survivorNumber - 1, 2].Length == 0)
                    {
                        traitsChecked[i] = true;
                        break;
                    }

                    // check against the searched traits
                    for (int j=0; j<3; j++)
                    {
                        // only check searched traits, that actually contain a value
                        if(activeTraits[survivorNumber - 1, j].Length > 0)
                            traitsChecked[i] |= CalculateLevenstienDistance(traitsList[i], activeTraits[survivorNumber - 1, j]) < stringDistanceTreshold;
                    }

                }

                // Count the traits the user searched for
                int searchedForTraits = 0;
                for(int i=0; i<3; i++)
                {
                    if(activeTraits[survivorNumber-1, i].Length > 0)
                    {
                        searchedForTraits++;
                    }
                }

                // Count the traits, that have been found (empty ones count as found)
                int foundActiveTraits = 0;
                for(int i=0; i<traitsChecked.Length; i++)
                {
                    if (traitsChecked[i])
                        foundActiveTraits++;
                }

                // only match if there are more found traits than searched ones (including the empty ones)
                match = foundActiveTraits >= searchedForTraits;

                /*
                foreach(Boolean check in traitsChecked)
                    match |= check;
                */

                if(match)
                {
                    sw.WriteLine("Found for survivor" + survivorNumber + "'" + activeTraits[survivorNumber - 1, 0] + "'" + activeTraits[survivorNumber - 1, 1] + "'" + activeTraits[survivorNumber - 1, 2] + "' in " + listToString(traitsList));
                }
                else
                {
                    sw.WriteLine("NOT Found for survivor" + survivorNumber + "'" + activeTraits[survivorNumber - 1, 0] + "'" + activeTraits[survivorNumber - 1, 1] + "'" + activeTraits[survivorNumber - 1, 2] + "' in " + listToString(traitsList));
                }

                sw.Flush();

            }

            return match;
        }

        private String listToString(List<String> list)
        {
            string result = "";
            foreach (String s in list)
                result += s;
            return result;
        }


        /* Old logic - just bullshit
        private bool TextMatch(int leftTrait, int topTrait, int leftSkill, int topSkill, Bitmap traitsOrg, Bitmap skills, int survivorNumber)
        {
            bool match;

            Bitmap traits = ApplyTresholdAndInvert(traitsOrg, 100, "testtrait" + leftTrait.ToString());
            //Bitmap skills = ApplyTresholdAndInvert(skillsOrg, 100, "testskill" + leftSkill.ToString());

            using (var objOcr = OcrApi.Create())
            { 
                objOcr.Init(Patagames.Ocr.Enums.Languages.English);
                string plainTextTraits = objOcr.GetTextFromImage(traits);
                string plainTextSkills = objOcr.GetTextFromImage(skills);
                sw.WriteLine("Plain Traits text: \n" + plainTextTraits);
                sw.Flush();
                string formattedTextTraits = Regex.Replace(plainTextTraits, @"\s+", "").ToUpper();
                string formattedTextSkills = Regex.Replace(plainTextSkills, @"\s+", "").ToUpper();

                if (ComputeStringDistance(activeTraits[survivorNumber - 1, 0], formattedTextTraits) <= (formattedTextTraits.Length - activeTraits[survivorNumber - 1, 0].Length) + 1 &&
                    ComputeStringDistance(activeTraits[survivorNumber - 1, 1], formattedTextTraits) <= (formattedTextTraits.Length - activeTraits[survivorNumber - 1, 1].Length) + 1 &&
                    ComputeStringDistance(activeTraits[survivorNumber - 1, 2], formattedTextTraits) <= (formattedTextTraits.Length - activeTraits[survivorNumber - 1, 2].Length) + 1 &&
                    ComputeStringDistance(activeSkills[survivorNumber - 1], formattedTextSkills) <= (formattedTextSkills.Length - activeSkills[survivorNumber - 1].Length) + 1)
                {
                    match = true;
                    sw.WriteLine("Found for survivor" + survivorNumber + "'" + activeTraits[survivorNumber - 1, 0] + "'" + activeTraits[survivorNumber - 1, 1] + "'" + activeTraits[survivorNumber - 1, 2] + "' in " + formattedTextTraits);
                }
                else
                {
                    match = false;
                    sw.WriteLine("NOT Found for survivor" + survivorNumber + "'" + activeTraits[survivorNumber - 1, 0] + "'" + activeTraits[survivorNumber - 1, 1] + "'" + activeTraits[survivorNumber - 1, 2] + "' in " + formattedTextTraits);
                    //sw.WriteLine("DesiredTraits: " + activeTraits[survivorNumber - 1, 0] + " -- " + activeTraits[survivorNumber - 1, 0] + " -- " + activeTraits[survivorNumber - 1, 0]);
                    //sw.WriteLine("Could not find traits:\n " + plainTextTraits + " \n" + formattedTextTraits);
                    //sw.WriteLine("Could not find skills:\n" + plainTextSkills + "\n" + formattedTextSkills);
                    //sw.WriteLine();
                }
                

                //sw.WriteLine(formattedTextTraits);
                //sw.WriteLine(formattedTextSkills);
                sw.Flush();

            }

            return match;
        }
        */

        public Bitmap ConvertToBitmap(string fileName)
        {
            Bitmap bitmap;
            using (Stream bmpStream = System.IO.File.Open(fileName, System.IO.FileMode.Open))
            {
                Image image = Image.FromStream(bmpStream);

                bitmap = new Bitmap(image);

            }
            return bitmap;
        }

        private void EnableTimer()
        {
            ////Program waits 50ms before iterating to prevent misreading by the OCR
            System.Threading.Thread.Sleep(50);

            //Disable reroll if control is held
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                Stop();
            }
            else if (reroll)
            {
                rerollTimer.Enabled = true;
            }
        }

        //Get the distance between two strings of different lengths
        public static int ComputeStringDistance(string input1, string input2)
        {
            var bounds = new { Height = input1.Length + 1, Width = input2.Length + 1 };
            int[,] matrix = new int[bounds.Height, bounds.Width];
            for (int height = 0; height < bounds.Height; height++)
            {
                matrix[height, 0] = height;
            }
            for (int width = 0; width < bounds.Width; width++)
            {
                matrix[0, width] = width;
            }
            for (int height = 1; height < bounds.Height; height++)
            {
                for (int width = 1; width < bounds.Width; width++)
                {
                    int cost = (input1[height - 1] == input2[width - 1]) ? 0 : 1;
                    int insertion = matrix[height, width - 1] + 1;
                    int deletion = matrix[height - 1, width] + 1;
                    int substitution = matrix[height - 1, width - 1] + cost;
                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));
                    if (height > 1 && width > 1 && input1[height - 1] == input2[width - 2] && input1[height - 2] == input2[width - 1])
                    {
                        distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
                    }
                    matrix[height, width] = distance;
                }
            }
            return matrix[bounds.Height - 1, bounds.Width - 1];
        }


        // Compute Levenstien Distance
        // from https://gist.github.com/Davidblkx/e12ab0bb2aff7fd8072632b396538560
        public static int CalculateLevenstienDistance(string source1, string source2) //O(n*m)
        {
            var source1Length = source1.Length;
            var source2Length = source2.Length;

            var matrix = new int[source1Length + 1, source2Length + 1];

            // First calculation, if one entry is empty return full length
            if (source1Length == 0)
                return source2Length;

            if (source2Length == 0)
                return source1Length;

            // If both are empty, just return 0
            if (source2Length == 0 && source1Length == 0)
                return 0;

            // Initialization of matrix with row size source1Length and columns size source2Length
            for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
            for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }

            // Calculate rows and collumns distances
            for (var i = 1; i <= source1Length; i++)
            {
                for (var j = 1; j <= source2Length; j++)
                {
                    var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            // return result
            return matrix[source1Length, source2Length];
        }

        private void Stop()
        {
            //Reset and disable the timer
            survivor = 1;
            rerollTimer.Interval = wait;
            reroll = false;
            rerollTimer.Enabled = false;
            ToggleButtons(true);
        }

        private void ToggleButtons(bool toggle)
        {
            //Enable and disable buttons
            btnStop.BeginInvoke((Action)delegate() { btnStop.Enabled = !toggle; });
            btnStart.BeginInvoke((Action)delegate() { btnStart.Enabled = toggle; });
        }

        private void SkillChanged(object sender, EventArgs e)
        {
            int index = 0;
            ComboBox cb = (ComboBox)sender;

            switch (cb.Name)
            {
                case "cbSurvivor1":
                    index = 0;
                    break;
                case "cbSurvivor2":
                    index = 1;
                    break;
                case "cbSurvivor3":
                    index = 2;
                    break;
            }

            activeSkills[index] = Regex.Replace(cb.GetItemText(cb.SelectedItem).ToUpper(), @"\s+", "");
        }

        private void TraitChanged(object sender, EventArgs e)
        {
            int i = 0, j = 0;
            ComboBox cb = (ComboBox)sender;

            switch (cb.Name)
            {
                case "cbS1Trait1":
                    i = 0;
                    j = 0;
                    break;
                case "cbS1Trait2":
                    i = 0;
                    j = 1;
                    break;
                case "cbS1Trait3":
                    i = 0;
                    j = 2;
                    break;
                case "cbS2Trait1":
                    i = 1;
                    j = 0;
                    break;
                case "cbS2Trait2":
                    i = 1;
                    j = 1;
                    break;
                case "cbS2Trait3":
                    i = 1;
                    j = 2;
                    break;
                case "cbS3Trait1":
                    i = 2;
                    j = 0;
                    break;
                case "cbS3Trait2":
                    i = 2;
                    j = 1;
                    break;
                case "cbS3Trait3":
                    i = 2;
                    j = 2;
                    break;
            }

            activeTraits[i, j] = Regex.Replace(cb.GetItemText(cb.SelectedItem).ToUpper(), @"\s+", "");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //activeSurvivors = new int[] { 1, 2, 3 };
            activeSurvivors = new int[] { 1, 1, 1 };
            survivor = 1;
            ToggleButtons(false);
            firstIteration = true;
            reroll = true;
            rerollTimer.Enabled = true;
        }

        private void SoD2Reroll_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private Bitmap ApplyTresholdAndInvert(Bitmap bitmap2, int threshold, String saveAs)
        {

            Bitmap bitmap = (Bitmap)bitmap2.Clone();

            for(int height = 0; height < bitmap.Height; height++)
            {
                for(int width = 0; width < bitmap.Width; width++)
                {
                    var brightness = (0.299 * bitmap.GetPixel(width, height).R + 0.587 * bitmap.GetPixel(width, height).G + 0.114 * bitmap.GetPixel(width, height).B);
                    if (brightness > threshold)
                    {
                        bitmap.SetPixel(width, height, Color.Black);
                    }
                    else
                    {
                        bitmap.SetPixel(width, height, Color.White);
                    }
                }
            }

            
            String directory = Directory.GetCurrentDirectory() + "\\testData\\" + saveAs + ".jpg";
            //sw.WriteLine("Saving Directory for BW pics: " + directory);

            bitmap.Save(directory);
            

            return bitmap;
        }

        private void btnTest_Click(object sender, EventArgs e)
        {

            //Wait
            System.Threading.Thread.Sleep(wait);

            //Take test images
            Bitmap traitsImg1 = Screenshot((int)Math.Round(resolution.Width / 5.47), (int)Math.Round(resolution.Height / 3.65), heightTrait);
            Bitmap traitsImg2 = Screenshot((int)Math.Round(resolution.Width / 1.995), (int)Math.Round(resolution.Height / 3.65), heightTrait);
            Bitmap traitsImg3 = Screenshot((int)Math.Round(4.1 * resolution.Width / 5), (int)Math.Round(resolution.Height / 3.65), heightTrait);
            Bitmap skillsImg1 = Screenshot(resolution.Width / 5, (int)Math.Round(resolution.Height / 1.55), heightSkill);
            Bitmap skillsImg2 = Screenshot((int)Math.Round(resolution.Width / 1.925), (int)Math.Round(resolution.Height / 1.55), heightSkill);
            Bitmap skillsImg3 = Screenshot((int)Math.Round(4.2 * resolution.Width / 5), (int)Math.Round(resolution.Height / 1.55), heightSkill);

            //Save test images
            try
            {
                traitsImg1.Save(Directory.GetCurrentDirectory() + "\\TestImages\\Survivor1Traits.jpg");
                traitsImg2.Save(Directory.GetCurrentDirectory() + "\\TestImages\\Survivor2Traits.jpg");
                traitsImg3.Save(Directory.GetCurrentDirectory() + "\\TestImages\\Survivor3Traits.jpg");
                skillsImg1.Save(Directory.GetCurrentDirectory() + "\\TestImages\\Survivor1Skills.jpg");
                skillsImg2.Save(Directory.GetCurrentDirectory() + "\\TestImages\\Survivor2Skills.jpg");
                skillsImg3.Save(Directory.GetCurrentDirectory() + "\\TestImages\\Survivor3Skills.jpg");
            } catch(Exception ex)
            {
                MessageBox.Show("Could not save images:" + Environment.NewLine + ex.ToString());
            }
            
            //Display finish notification
            MessageBox.Show("Test complete." + Environment.NewLine + "Images saved in " + Directory.GetCurrentDirectory() + "\\TestImages");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void nudWait_ValueChanged(object sender, EventArgs e) 
        { 
            wait = Convert.ToInt16(nudWait.Value * 100); 
        }

        private void cbResolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] dimensions = cbResolution.GetItemText(cbResolution.SelectedItem).Split('x');
            resolution.Width = Convert.ToInt16(dimensions[0]);
            resolution.Height = Convert.ToInt16(dimensions[1]);
        }
    }
}