/****************************************************************************
* Generates Images representing Cards Against Humanity (CaH) Cards
* They are easily imported into TableTop Simulator (TTS)
*   - imported as a Custom Deck (10x7)
*   - you will have to specify the Back Face from ./CardBacks/*.png
* You can specify the text you want on your cards by creating text files in
*   - ./CardTextBlack/<CaH_Pack_Name>.txt for the Black CaH Cards
*   - ./CardTextWhite/<CaH_Pack_Name>.txt for the White CaH Cards
* If you just want one deck, you can specify them all in one text file, but
* if you split them into multiple files, those will act as separate "Packs."
* Each Card is labeled at the bottom with the <CaH_Pack_Name>
* 
* Author: CynicPlacebo (@CynicPlacebo on Git & Twitter)
* License: You can use/modify as you like, just don't sell it.
*          I'd also appreciate a shoutout, but it isn't required.
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace CahGen
{
    /**
     * This Class is the main entry point for CahGenerator
     * It Creates a CahGenerator, and Generates all Decks/Cards
     * It writes them to file in a way that is easy to import into TableTop Simulator
     */
    class Program
    {
        public static int debugLevel = 1; //TODO: set debug level to 0
        private static String logPath = "execution_log.txt"; //Will be changed to full path in Main()
        public static String slash = System.IO.Path.DirectorySeparatorChar.ToString();

        static void Main(string[] args)
        {
            //Get Directory of this Script, as output will be in subdirs
            StackTrace st = new StackTrace(new StackFrame(true));
            StackFrame sf = st.GetFrame(0);
            String currFile = sf.GetFileName();
            String rootDir = System.IO.Path.GetDirectoryName(currFile);
            logPath = rootDir + slash + logPath;
            out2("Working Directory: " + rootDir);

            //Wipe out the log file for a new run:
            using (StreamWriter sw = new StreamWriter(logPath, false))
            {
                sw.WriteLine("Execution started at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n");
            }


            //Generate All Black and White Decks
            CahGenerator cah = new CahGenerator(rootDir);
            Console.WriteLine("Generating Black Decks...");
            int numBlack = cah.generateBlackDecks();
            Console.WriteLine("Generating White Decks...");
            int numWhite = cah.generateWhiteDecks();

            //Let the user see the script output before you close
            Console.WriteLine("\n\nCompleted Generation.\nDetails of Execution can be found in:\n - " + logPath);
            Console.WriteLine("\n\nPress any key to close");
            Console.ReadKey();
        }

        /**
         * Outputs to the Log File *IF* the priority level is below the current debugLevel
         * @param message: the string to output
         * @param level: the debug level (0, 1, 2, 3, etc...)
         */
        public static void output(String message, int level)
        {
            if (level > debugLevel)
                return; //Skip, because priority isn't <= to debug level
            //using (StreamWriter sw = File.AppendText(logPath)) {
            using (StreamWriter sw = new StreamWriter(logPath, true)){ 
                sw.WriteLine(message);
            }
        }
        public static void output(String m) { output(m, 1); } //Assume level 1 (debugging might hide it)
        public static void out0(String m) { output(m, 0); } //Level 0 always outputs
        public static void out1(String m) { output(m, 1); } //Level 1 usually outputs
        public static void out2(String m) { output(m, 2); } //Level 1 infrequently outputs
        public static void out3(String m) { output(m, 3); } //Level 1 almost never outputs
    }


    /**
     * This Class Generates Images of Cah Cards and saves them to file.
     * It creates 70-card Deck Sheets for eaasy import into TTS, but it
     * also creates individual card images for non-TTS use.
     */
    class CahGenerator
    {
        /********** Static Config Vars **********/
        //Colors for Cards (Dark Text and Background, Light Text and Background)
        public static Color cDark = Color.Black; //Color.FromArgb(5, 5, 5);
        public static Color cLight = Color.FromArgb(242, 236, 220); //#f2ecdc
        //Card Dimensions
        public static int cardH = 585;
        public static int cardW = 409;
        public static int cardPadTop = 24;
        public static int cardPadSide = 15;
        //Deck & Deck Sheet Dimensions
        public static int deckCols = 10; //TTS only allows deck sheets of up to 10x7
        public static int deckRows = 7;
        public static int maxDeckSize = deckCols * deckRows; //10x7 sheets can have up to 70 cards
        public static int sheetH = cardH * deckRows; //Sheet is 4090px by 4095px
        public static int sheetW = cardW * deckCols;
        //Misc
        public static String slash = System.IO.Path.DirectorySeparatorChar.ToString();

        /********** Dynamic Member Vars **********/
        private int totalCards = 0;
        private int totalDecks = 0;
        //Directories (all will be full paths after Constructor)
        private String dir = ""; //Root directory where this program runs
        private String dirBlack = "CardTextBlack"; //for Input Text of Black Cards
        private String dirOutCards = "GeneratedCards"; //Output of Individual Card Images
        private String dirOutDecks = "GeneratedDecks"; //Output of Deck Sheets
        private String dirWhite = "CardTextWhite"; //for Input Text of White Cards
        //Paths to Back Faces of Cards (for use in summary output)
        private String pathToBlackBack = "CardBacks" + slash + "BackBlack.png";
        private String pathToWhiteBack = "CardBacks" + slash + "BackWhite.png";


        /**
         * Constructs the CahGenerator object
         * @param rootDir: The folder where this program is running (under which we expect the cards to be output)
         */
        public CahGenerator(String rootDir)
        {
            dir = rootDir;
            //Force Full Paths on all directory member vars
            dirBlack = dir + slash + dirBlack;
            dirOutCards = dir + slash + dirOutCards;
            dirOutDecks = dir + slash + dirOutDecks;
            dirWhite = dir + slash + dirWhite;
            pathToBlackBack = dir + slash + pathToBlackBack;
            pathToWhiteBack = dir + slash + pathToWhiteBack;
            //Ensure Folders for Output Exist
            if (!Directory.Exists(dirOutCards))
                Directory.CreateDirectory(dirOutCards);
            if (!Directory.Exists(dirOutDecks))
                Directory.CreateDirectory(dirOutDecks);
            Program.out2("CahGenerator Directories: \n - " + dirBlack + "\n - " + dirWhite + "\n - " + dirOutDecks + "\n");
        }

        /**
         * Creates an Image for a Single Card and writes it to file
         * @param name: Name for this Deck Group (which CaH expansion pack is it)
         * @param suffix: "B" or "W" for Black/White cards, respectively
         * @param text: what to write on this card
         * @return true if successful (TODO: track fail case)
         */
        private bool DrawCard(String name, String suffix, String text)
        {
            Color cBack = getBackColor(suffix);
            Color cText = getTextColor(suffix);
            Brush textBrush = new SolidBrush(cText);
            Font font = new Font("Arial", 30); //"Fantasy" pack has the longest black card. Check it to ensure font small enough (14 is good)
            Font fontLabel = new Font("Arial", 12);
            //Create Image with Card Size
            Image img = new Bitmap(cardW, cardH);
            Graphics drawing = Graphics.FromImage(img); //and set options below
            drawing.Clear(cBack);
            drawing.SmoothingMode = SmoothingMode.AntiAlias;
            drawing.TextRenderingHint = TextRenderingHint.AntiAlias;

            //TableTopSim Standard Card Size is 409x585 (max size for 10x7 deck sheets)
            int innerH = cardH - (2 * cardPadTop);
            int innerW = cardW - (2 * cardPadSide);
            int centerish = (cardW / 2) - cardPadSide;
            Rectangle rect = new Rectangle(cardPadSide, cardPadTop, innerW, innerH);
            Rectangle rectLabel = new Rectangle(cardPadSide, innerH, innerW, cardH - innerH); //tucked at bottom right

            //Insert the Text into the Image with the Font, Brush and Bounding Rectangle
            drawing.DrawString(text, font, textBrush, rect); //Draw main text
            drawing.DrawString(name, fontLabel, textBrush, rectLabel); //Draw label text
            drawing.Save();

            //Free Memory and Return Final Image
            textBrush.Dispose();
            drawing.Dispose();

            //Write to file
            String path = dirOutCards + slash + name + "_" + suffix + "_" + totalCards + ".png";
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            MemoryStream memStream = new MemoryStream();
            img.Save(memStream, ImageFormat.Png); //Use PNG because the 10x7 deck sheets are ALWAYS Png (avoid double compression)
            memStream.WriteTo(fs);

            return true;
        }

        /**
         * Creates the Images for a Deck Sheet and writes it to file
         * @param name: Name for this Deck Group (which CaH expansion pack is it)
         * @param suffix: "B" or "W" for Black/White cards, respectively
         * @param part: 0 if this is an entire deck, otherwise an integer
         *     representing which Deck Part this is (1 through N).
         *     TTS only accepts 70 cards per deck sheet. This just lets
         *     us number the output Deck Sheets uniquely.
         * @param texts: array of text. Each item is 1 saying to write on a card
         * @return true if successful (TODO: track fail case)
         */
        private bool DrawDeckBatch(String name, String suffix, int part, String[] texts)
        {
            Color cBack = getBackColor(suffix);
            Color cText = getTextColor(suffix);
            Brush textBrush = new SolidBrush(cText);
            Font font = new Font("Arial", 30); //"Fantasy" pack has the longest black card. Check it to ensure font small enough (30 seems good)
            Font fontLabel = new Font("Arial", 12);
            //Create Image with size for 10x7 grid of Cards (some may be blank)
            Image img = new Bitmap(sheetW, sheetH);
            Graphics drawing = Graphics.FromImage(img); //and set options below
            drawing.Clear(cBack); //Background Color
            drawing.SmoothingMode = SmoothingMode.AntiAlias;
            drawing.TextRenderingHint = TextRenderingHint.AntiAlias;

            //Loop through all texts as row/cols
            int cardIndex = 0;
            int numCards = texts.Length;
            int x = 0; //Left coordinate of Card's Rectangle
            int y = 0; //Top coordinate of Card's Rectangle
            int innerH = cardH - (2 * cardPadTop);
            int innerW = cardW - (2 * cardPadSide);
            int centerish = (cardW / 2) - cardPadSide;
            for (int row = 0; row < deckRows && cardIndex < numCards; ++row)
            {
                y = row * cardH;
                for (int col = 0; col < deckCols && cardIndex < numCards; ++col)
                {
                    x = col * cardW;
                    Rectangle rect = new Rectangle(x + cardPadSide, y + cardPadTop, innerW, innerH);
                    Rectangle rectLabel = new Rectangle(x + cardPadSide, y + innerH, innerW, cardH - innerH); //tucked at bottom left

                    //Insert the Text into the Image with the Font, Brush and Bounding Rectangle
                    drawing.DrawString(texts[cardIndex], font, textBrush, rect); //Draw main text
                    drawing.DrawString(name, fontLabel, textBrush, rectLabel); //Draw label text
                    drawing.Save();

                    cardIndex++;
                }
            }

            //Free Memory, as it is all Saved in the Image
            textBrush.Dispose();
            drawing.Dispose();

            //Name output file as <Name>_<DeckColor>_<PartX>_10x7_total_<TOTAL_CARDS>.png
            String partString = "";
            if (part > 0)
                partString = "_part" + part;
            String path = dirOutDecks + slash + name + "_" + suffix + partString + "_10x7_total_" + cardIndex + ".png";
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write); //Will overwrite
            MemoryStream memStream = new MemoryStream();
            img.Save(memStream, ImageFormat.Png); //Use PNG because the 10x7 deck sheets are ALWAYS Png (avoid double compression)
            memStream.WriteTo(fs);

            return true;
        }

        /**
         * Calls generateDecks() with defaults for Black Decks
         * @return: Returns number of Decks generated
         */
        public int generateBlackDecks() {
            return generateDecks(dirBlack, "B");
        }
        /**
         * Calls generateDecks() with defaults for White Decks
         * @return: Returns number of Decks generated
         */
        public int generateWhiteDecks()
        {
            return generateDecks(dirWhite, "W");
        }
        /**
         * Generates ALL Decks from *.txt files in the specified directory (./CardTextBlack or ./CardTextWhite)
         * @param dirInput: Full path of *.txt files to read
         * @param suffix: "B" or "W" for Black/White to be added to the output file
         * @return Returns the number of Decks Generated
         */
        private int generateDecks(String dirInput, String suffix) {
            //Scan for Input Files (Text for the Cards)
            String[] textFiles = Directory.GetFiles(dirInput, "*.txt");

            //Process Black Decks
            foreach (String filepath in textFiles) {
                String name = System.IO.Path.GetFileNameWithoutExtension(filepath);
                String[] texts = parseDeckFile(filepath);
                Program.out1("Total Lines for " + name + ": " + texts.Length + "\n");
                Program.out3("Call generateDeck() with " + filepath);
                generateDeck(name, suffix, texts);
            }

            Program.out0("\nGenerated " + totalCards + " cards in " + totalDecks + " decks.");
            Program.out0("Images were written to these folders:\n - " + dirOutCards + "\n - " + dirOutDecks);
            Program.out0("The backs of the cards are in: " + dir + slash + "CardBacks" + slash);
            return totalDecks;
        }

        /**
         * Counts the total number of cards that have been created and triggers DrawCard()
         * @param name: Name of this Deck Pack
         * @param suffix: "B" or "W" for Black/White cards, respectively
         * @param text: the text to write on this card
         * @return true if successful (TODO: track fail case)
         */
        private bool generateCard(String name, String suffix, String text)
        {
            //Set Text and Colors and Write to File
            totalCards++;
            DrawCard(name, suffix, text);

            return true;
        }
        /**
         * Generates Cards with the specified group name, suffix, and texts
         * @param name: Name for this Group of Cards, as determined by the name of the File in ./CardTextBlack or ./CardTextWhite
         * @param suffix: "B" or "W" for Black/White cards, respectively
         * @param texts: array of text to show on the cards in this deck
         * @return true if successful (TODO: track fail case)
         */
        private bool generateDeck(String name, String suffix, String[] texts)
        {
            Program.out2("=============== Generate Cards (" + name + ") ===============");
            int numCards = texts.Length;
            Program.out2("Num Cards: " + numCards);
            Color cBack = getBackColor(suffix);
            Color cText = getTextColor(suffix);

            //Create Sheets of 70 cards at a time (TTS likes 10x7 sheets)
            int cardIndex = 0;
            int numBatches = (numCards / maxDeckSize) + 1;
            int part = 0;
            Program.out2("...num batches = " + numBatches);
            for (int i = 0; i < numBatches; ++i)
            {
                if (numBatches > 1)
                    part++;
                List<String> batch = new List<String>();
                int cardsInBatch = maxDeckSize;
                if (i == numBatches - 1)
                    cardsInBatch = numCards % maxDeckSize;
                int batchEndIndex = cardIndex + cardsInBatch - 1; //0-based, so -1
                Program.out3("===> Processing " + cardIndex + " - " + batchEndIndex + " [texts.Length = " + texts.Length + "]");
                for(int j = 0; j < cardsInBatch; ++j)
                {
                    batch.Add(texts[cardIndex]);
                    //Also Create the Individual Card
                    generateCard(name, suffix, texts[cardIndex]);
                    cardIndex++;
                }
                DrawDeckBatch(name, suffix, part, batch.ToArray());
            }

            totalDecks++;
            return true;
        }

        /**
         * Returns the Color for the Background of a Black or White card
         * @param suffix: "B" or "W" for a Black/White card, respectively
         * @return Color for Card Background
         */
        private Color getBackColor(String suffix)
        {
            if ("B".CompareTo(suffix) == 0)
                return cDark; //It's a Black Card
            return cLight; //It's a White Card
        }
        /**
         * Returns the Color for the Background of a Black or White card
         * @param suffix: "B" or "W" for a Black/White card, respectively
         * @return Color for Card Text
         */
        private Color getTextColor(String suffix)
        {
            if ("B".CompareTo(suffix) == 0)
                return cLight; //It's a Black Card with white text
            return cDark; //It's a White Card with black text
        }

        /**
         * Parses the text of a file meant to be a Deck
         * Each line is 1 card face
         * It ignores Blank Lines and lines starting with //
         * It replaces <br> with \n
         * (TODO: Other Formatting/Replacements?)
         * @param filepath: the full path of the file to read
         * @return an array of Strings representing each card to create
         */
        private String[] parseDeckFile(String filepath)
        {
            List<String> rtn = new List<string>();
            String[] lines = File.ReadAllLines(filepath);
            foreach (String line in lines)
            {
                String temp = line.Trim();
                //Eliminate Blank Lines
                if (temp.Length == 0)
                    continue; //Skip this blank line
                //Eliminate Comment Lines
                if (temp.StartsWith("//"))
                    continue; //Skip this comment line

                //Format Line Breaks <br>
                temp = temp.Replace("<br>", "\n");

                //Save this Card to the return deck
                rtn.Add(temp);
            }
            return rtn.ToArray();
        }
    }
}
