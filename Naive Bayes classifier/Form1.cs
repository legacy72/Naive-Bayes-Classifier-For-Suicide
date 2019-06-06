using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows.Forms;

namespace Naive_Bayes_classifier
{
    public partial class Form1 : Form
    {
        string _path = @"C:\Users\vladr\source\repos\Naive-Bayes-classifier-suicide\data\training_data.json";
        Dictionary<string, int> SuicideDict = new Dictionary<string, int>(); // словарь суицида
        Dictionary<string, int> NormalDict = new Dictionary<string, int>(); // словарь несуицида
        double SuicideFrequencies = 0; // количество документов в обучающей выборке принадлежащих классу суицид;
        double NormalFrequencies = 0; // количество документов в обучающей выборке принадлежащих классу несуицид;

        public Form1()
        {
            InitializeComponent();

            TrainingData();
        }       
        
        // Вывод словаря для тестирования
        void PrintDict(Dictionary<string, int> dictionary)
        {
            string s = "";
            foreach (KeyValuePair<string, int> kvp in dictionary)
            {
                s += string.Format("{0}: {1}", kvp.Key, kvp.Value) + "\n";
            }
            MessageBox.Show(s);
        }
        // чтение json данных для тренировки из файла
        Data ReadFile()
        {
            string text = "";
            string jsonFromFile;
            using (var reader = new StreamReader(_path))
            {
                jsonFromFile = reader.ReadToEnd();
            }

            text = jsonFromFile;

            var dataJSON = JsonConvert.DeserializeObject<Data>(jsonFromFile);

            return dataJSON;
        }

        string PrepareText(string text)
        {
            string testText = "";
            string[] words = text.ToLower().Split();
            foreach (string word in words)
            {
                testText += Porter.TransformingWord(word) + " ";
            }
            return testText.Trim();
        }

        void TrainingData()
        {
            Data data = ReadFile();
            FillDictionary(data);
        }


        // заполнение словарей словами, которые принадлежат к конкретной категории
        void FillDictionary(Data data)
        {
            string[] words;
            foreach(Suicide suicide in data.suicide)
            {
                SuicideFrequencies++;
                words = suicide.text.Split();
                foreach (string word in words)
                {
                    if (SuicideDict.ContainsKey(word))
                    {
                        SuicideDict[word] += 1;
                    }
                    else
                    {
                        SuicideDict.Add(word, 1);
                    }
                }
            }
            foreach (Normal normal in data.normal)
            {
                NormalFrequencies++;
                words = normal.text.Split();
                foreach (string word in words)
                {
                    if (NormalDict.ContainsKey(word))
                    {
                        NormalDict[word] += 1;
                    }
                    else
                    {
                        NormalDict.Add(word, 1);
                    }
                }
            }
        }

        // Вычисление вероятности, что сообщение суицидальное
        double GetProbabilitySuicide(string testString, int countUniqueKeys)
        {
            string[] str = testString.Split();

            List<int> W = new List<int>(); // сколько раз слово встречалось в классе суицид

            foreach (string s in str)
            {
                if (SuicideDict.ContainsKey(s))
                {
                    W.Add(SuicideDict[s]);
                }
                else
                {
                    W.Add(0);
                }
            }

            double probSuicide = Math.Log(SuicideFrequencies / (SuicideFrequencies + NormalFrequencies));
            foreach (int w in W)
            {
                probSuicide += Math.Log((1 + w) / (double)(countUniqueKeys + SuicideDict.Count()));
            }

            return probSuicide;
        }

        // Вычисление вероятности, что сообщение не суицид
        double GetProbabilityNormal(string testString, int countUniqueKeys)
        {
            string[] str = testString.Split();

            List<int> W = new List<int>(); // сколько раз слово встречалось в классе не суицид

            foreach (string s in str)
            {
                if (NormalDict.ContainsKey(s))
                {
                    W.Add(NormalDict[s]);
                }
                else
                {
                    W.Add(0);
                }
            }

            double probNormal = Math.Log(NormalFrequencies / (SuicideFrequencies + NormalFrequencies));
            foreach (int w in W)
            {
                probNormal += Math.Log((1 + w) / (double)(countUniqueKeys + NormalDict.Count()));
            }

            return probNormal;
        }

        // Подсчет кол-ва уникальных слов во всей выборке
        int GetCountUniqueKeys()
        {
            List<string> keyListSuicide = new List<string>(SuicideDict.Keys);
            List<string> keyListNormal = new List<string>(NormalDict.Keys);
            keyListSuicide.AddRange(keyListNormal);

            keyListSuicide = keyListSuicide.Distinct().ToList();

            return keyListSuicide.Count();
        }

        // Формирование вероятностного пространства
        double FormationOfProbabilisticSpace(double a, double b)
        {
            return Math.Exp(a) / (Math.Exp(a) + Math.Exp(b));
        }

        // сама проверка на суицид
        void Test(string testString)
        {
            // количество уникальных слов в обеих выборках
            int countUniqueKeys = GetCountUniqueKeys();

            // подсчет шанса что суицид (не вероятностное пространство)
            double resSuicide = GetProbabilitySuicide(testString, countUniqueKeys);

            // подсчет шанса что не суицид (не вероятностное пространство)
            double resNormal = GetProbabilityNormal(testString, countUniqueKeys);

            double probSuicide = FormationOfProbabilisticSpace(resSuicide, resNormal);
            double probNormal = FormationOfProbabilisticSpace(resNormal, resSuicide); // ну либо 1 - probresSuicide

            ShowResultGraph(probSuicide, probNormal);
        }

        //Вывод результата на диаграмму
        void ShowResultGraph(double probSuicide, double probNormal)
        {
            chart1.Series[0].Points.Clear();
            chart1.Series["probSuicide"].Points.AddXY("суицид", probSuicide * 100);
            chart1.Series["probSuicide"].Points.AddXY("не суицид", probNormal * 100);
        }



        // опен файл диалог (выбираем текстовик на проверку)
        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();

            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "Рабочий стол:\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                    var fileStream = openFileDialog.OpenFile();

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        fileContent = reader.ReadToEnd();
                        LoadText(fileContent);
                    }
                }
            }
        }

        private void LoadText(string fileContent)
        {
            richTextBox2.Text = fileContent.ToLower();

            richTextBox2.SelectionStart = 0;
            richTextBox2.SelectionLength = 0;
            richTextBox2.SelectionColor = Color.Black;

            DistinguishWords(richTextBox2.Text);
        }

        // Выделение слов цветом
        private void DistinguishWords(string text)
        {
            string[] words = text.Split(' ');

            var intersectedWordsIEnum = words.Intersect(SuicideDict.Keys, StringComparer.OrdinalIgnoreCase);
            string[] intersectedWords = String.Join(" ", intersectedWordsIEnum).Split(' ');


            foreach (string word in intersectedWords)
            {
                HighlightText(word, Color.Red); // выделение слов цветом
            }

            var anotherWordsIEnum = words.Except(intersectedWords);
            string[] anotherWords = String.Join(" ", anotherWordsIEnum).Split(' ');
        }

        private void HighlightText(string word, Color color)
        {
            if (word == string.Empty)
                return;

            int s_start = richTextBox2.SelectionStart, startIndex = 0, index;

            while ((index = richTextBox2.Text.IndexOf(word, startIndex)) != -1)
            {
                richTextBox2.Select(index, word.Length);
                richTextBox2.SelectionColor = color;

                startIndex = index + word.Length;
            }

            richTextBox2.SelectionStart = s_start;
            richTextBox2.SelectionLength = 0;
            richTextBox2.SelectionColor = Color.Black;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string testText = PrepareText(richTextBox1.Text);
            richTextBox2.Text = testText;
            Test(testText);
            DistinguishWords(richTextBox2.Text);
        }
    }
}
