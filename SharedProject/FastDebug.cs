using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;

namespace SharedProject
{
    internal class FastDebug
    {
        /// <summary>
        /// Prints the message in debug screen in visual studio. Short way for debugging.
        /// </summary>
        public static void p(object message)
        {
            Debug.WriteLine(message);
        }
        /// <summary>
        /// show warning message box with a message
        /// </summary>
        public static void w(object message)
        {
            string text = message.ToString();
            text = ToSentenceCase(text);
#if CLIENT
            MessageBox.Show(text);
#endif
        }
        static string ToSentenceCase(string input)
        {
            TextInfo textInfo = new CultureInfo("tr-TR", false).TextInfo;

            // Cümleleri noktalardan ayır
            string[] sentences = input.Split('.');

            for (int i = 0; i < sentences.Length; i++)
            {
                // Boşlukları temizle
                sentences[i] = sentences[i].Trim();

                // Cümle başındaki harfi büyük yap
                if (!string.IsNullOrEmpty(sentences[i]))
                {
                    sentences[i] = char.ToUpper(sentences[i][0]) + sentences[i].Substring(1);
                }
            }

            // Cümleleri tekrar birleştir
            return string.Join(". ", sentences);
        }
    }
}
