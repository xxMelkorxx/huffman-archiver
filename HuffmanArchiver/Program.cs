using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace HuffmanArchiver
{
    public static class ArchiverHuffman
    {
        //Сохраняет дерево
        [Serializable]
        public class BinaryTree<T>
        {
            public BinaryTreeNode<T> Root { get; set; }

            public BinaryTree(BinaryTreeNode<T> root)
            {
                Root = root;
            }
        }

        //Сохраняет дерево для обратного хода
        [Serializable]
        public class BinaryTreeNode<T>
        {
            public BinaryTreeNode<T> LeftChild { get; set; }
            public BinaryTreeNode<T> RightChild { get; set; }
            public T Value { get; set; }

            public BinaryTreeNode(T value)
            {
                Value = value;
            }

            public BinaryTreeNode(T value, BinaryTreeNode<T> leftChild, BinaryTreeNode<T> rightChild)
            {
                Value = value;
                LeftChild = leftChild;
                RightChild = rightChild;
            }
        }

        private const string UncodedText = @"D:\SerBor\Dev\huffman-archiver\UncodedText.txt";
        private const string EncodedText = @"D:\SerBor\Dev\huffman-archiver\EncodedText.txt";
        private const string DecodedText = @"D:\SerBor\Dev\huffman-archiver\DecodedText.txt";
        
        private static void Main()
        {
            // Исходная таблица
            var table = CreateProbabilityTable(UncodedText, out var symbolCount);
            Console.WriteLine("Таблица вероятностей символов в тексте:");
            PrintDictionary(table);
            Console.WriteLine("\nКоличество символов в тексте: {0}", symbolCount);
            Console.WriteLine("Количество уникальных символов: {0}", table.Count);

            //Закодированная таблица
            var huffmanTree = CreateHuffmanTree(table);
            var codeTable = CreateCodeTable(huffmanTree, table.Keys.ToList());
            Console.WriteLine("\nКодовая таблица:");
            PrintDictionary(codeTable);

            //Кодирование текста
            EncodeFile(UncodedText, EncodedText, 4, out var comresCoef);
            Console.WriteLine("Коэффициент сжатия: K = {0}", comresCoef);

            //Декодирование текста
            DecodeFile(EncodedText, DecodedText, 8, 10);
            
            //Итоговая информация.
            Console.WriteLine("\nРазмер исходного файла составляет {0} байт", new FileInfo(UncodedText).Length);
            Console.WriteLine("Размер сжатого файла составляет {0} байт", new FileInfo(EncodedText).Length);
            Console.WriteLine("Размер декодированного файла составляет {0} байт", new FileInfo(DecodedText).Length);
        }

        /// <summary>
        /// Создание таблицы: символ - вероятность
        /// <param name="file"></param>
        /// <param name="symbolCount"></param>
        /// </summary>
        /// <returns></returns>
        private static Dictionary<char, double> CreateProbabilityTable(string file, out int symbolCount)
        {
            symbolCount = 0;
            if (File.Exists(file))
            {
                //Первичная таблица: символ - количетво символов
                var first = new Dictionary<char, int>();
                //Вторичная таблица: символ - вероятность символа
                var second = new Dictionary<char, double>();
                //Чтение файла
                var reader = new StreamReader(file);

                int temp;
                while ((temp = reader.Read()) > -1)
                {
                    symbolCount++;
                    if (!first.ContainsKey((char)temp))
                        first.Add((char)temp, 1);
                    else first[(char)temp]++;
                }
                reader.Close();

                foreach (var i in first)
                    second.Add(i.Key, (double)i.Value / symbolCount);

                return second;
            }
            
            //На случай, если файла не существует
            throw new FileNotFoundException();
        }

        /// <summary>
        /// Вывод словаря на экран
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dict"></param>
        private static void PrintDictionary<K, V>(Dictionary<K, V> dict)
        {
            foreach (var i in dict)
                if (typeof(V).Equals(typeof(double)) || typeof(V).Equals(typeof(float)))
                    Console.WriteLine("{0} \t {1:f10}", i.Key, i.Value);
                else
                    Console.WriteLine("{0} \t {1}", i.Key, i.Value);
        }

        /// <summary>
        /// Создаёт бинарное дерево Хаффмана
        /// </summary>
        /// <param name="slovar">Словарь: символ - вероятность</param>
        /// <returns></returns>
        private static BinaryTree<string> CreateHuffmanTree(Dictionary<char, double> slovar)
        {
            //Бинарное дерево
            var binaryTree = new Dictionary<string, double>();
            foreach (var i in slovar)
                binaryTree.Add(i.Key.ToString(), i.Value);

            var nodeList = new Dictionary<string, BinaryTreeNode<string>>();
            foreach (var i in binaryTree.Keys)
                nodeList.Add(i, new BinaryTreeNode<string>(i));

            while (binaryTree.Count > 1)
            {
                //Вспомогательный массив, который отбирает пару символов с наименьшей вероятностью
                var helpmass = binaryTree.OrderBy(pair => pair.Value).Take(2).ToArray();
                //Скревляет эту пару символов, тем самым создаёт узел
                var s1 = helpmass[0].Key;
                var s2 = helpmass[1].Key;
                var s3 = string.Concat(s1, s2);

                var d1 = helpmass[0].Value;
                var d2 = helpmass[1].Value;

                binaryTree.Remove(s1);
                binaryTree.Remove(s2);
                binaryTree.Add(s3, d1 + d2);

                nodeList.TryGetValue(s1, out var leftChild);
                nodeList.TryGetValue(s2, out var rightChild);
                nodeList.Remove(leftChild.Value);
                nodeList.Remove(rightChild.Value);
                nodeList.Add(s3, new BinaryTreeNode<string>(s3, leftChild, rightChild));
            }

            nodeList.TryGetValue(binaryTree.ElementAt(0).Key, out var root);

            return new BinaryTree<string>(root);
        }

        /// <summary>
        /// Создание кодовой таблицы
        /// </summary>
        /// <param name="binaryTree">Бинарное дерево</param>
        /// <param name="symbols">Символы словаря</param>
        /// <returns></returns>
        private static Dictionary<char, string> CreateCodeTable(BinaryTree<string> binaryTree, List<char> symbols)
        {
            var codeTable = new Dictionary<char, string>();
            //Присваивание символам бинарный код
            foreach (var i in symbols)
            {
                var code = new StringBuilder();
                var temp = binaryTree.Root;
                while (temp.Value.Length > 1)
                    if (temp.LeftChild.Value.Contains(i))
                    {
                        code.Append("0");
                        temp = temp.LeftChild;
                    }
                    else
                    {
                        code.Append("1");
                        temp = temp.RightChild;
                    }

                codeTable.Add(i, code.ToString());
            }

            return codeTable;
        }

        /// <summary>
        /// Кодирование текста в файле
        /// </summary>
        /// <param name="uncodedText">Файл с незакодированым текстом</param>
        /// <param name="encodedText">Файл для закодированного текста</param>
        /// <param name="bufferSize"></param>
        /// <param name="compresCoef">Коэффициент сжатия</param>
        private static void EncodeFile(string uncodedText, string encodedText, int bufferSize, out double compresCoef)
        {
            if (File.Exists(uncodedText))
            {
                var reader = new StreamReader(uncodedText);
                var writer = File.Create(encodedText);
                var binaryCode = new StringBuilder();

                var table = CreateProbabilityTable(uncodedText, out var symbolCount);
                var huffmanTree = CreateHuffmanTree(table);
                var codeTable = CreateCodeTable(huffmanTree, table.Keys.ToList());

                //Сохраняем дерево
                var saveTree = new BinaryFormatter();
                saveTree.Serialize(writer, huffmanTree);

                int temp;
                while ((temp = reader.Read()) > -1)
                {
                    var symbol = Convert.ToChar(temp);
                    codeTable.TryGetValue(symbol, out var code);
                    binaryCode.Append(code);
                    if (binaryCode.Length > bufferSize * 8)
                    {
                        var bytes = StringToByte(binaryCode.ToString().Substring(0, bufferSize * 8));
                        binaryCode.Remove(0, bufferSize * 8);
                        foreach (var i in bytes)
                            writer.WriteByte(i);
                    }
                }

                if (binaryCode.Length != 0)
                {
                    var bytes = StringToByte(binaryCode.ToString().PadRight(bufferSize * 8, '0'));
                    foreach (var i in bytes)
                        writer.WriteByte(i);
                }

                writer.Close();
                reader.Close();

                //Коэффициент сжатия
                compresCoef = (double)new FileInfo(uncodedText).Length / new FileInfo(encodedText).Length;
            }
            else throw new FileNotFoundException();
        }

        /// <summary>
        /// Декодирование текста, закодированный с помощью алгоритма Хаффмана.
        /// </summary>
        /// <param name="encodedText">Файл для закодированного текста</param>
        /// <param name="decodedText">Файл для раскодированого текста</param>
        /// <param name="readBufferSize"></param>
        /// <param name="writeBuffersize"></param>
        private static void DecodeFile(string encodedText, string decodedText, int readBufferSize, int writeBuffersize)
        {
            if (File.Exists(encodedText))
            {
                var decodedStr = new StringBuilder();
                var binaryCode = new StringBuilder();
                var reader = File.OpenRead(encodedText);
                var writer = File.Create(decodedText);

                var breader = new BinaryReader(reader);
                var saveTree = new BinaryFormatter();
                var huffmanTree = (BinaryTree<string>)saveTree.Deserialize(reader);

                while (breader.BaseStream.Position != breader.BaseStream.Length)
                {
                    var buffer = breader.ReadBytes(readBufferSize);
                    foreach (var i in buffer)
                        binaryCode.Append(Convert.ToString(i, 2).PadLeft(8, '0'));
                    var temp = binaryCode.ToString();
                    while (temp.Length >= readBufferSize)
                    {
                        try
                        {
                            decodedStr.Append(DecodeSymbol(ref temp, huffmanTree));
                            binaryCode = new StringBuilder(temp);
                        }
                        catch (Exception)
                        {
                            break;
                        }

                        if (decodedStr.Length >= writeBuffersize)
                        {
                            var writeBuffer = Encoding.Default.GetBytes(decodedStr.ToString());
                            writer.Write(writeBuffer, 0, writeBuffer.Length);
                            decodedStr.Remove(0, writeBuffersize);
                        }
                    }
                }

                var writeBuffer2 = Encoding.Default.GetBytes(decodedStr.ToString());
                writer.Write(writeBuffer2, 0, writeBuffer2.Length);

                breader.Close();
                reader.Close();
                writer.Close();
            }
            else throw new FileNotFoundException();
        }

        /// <summary>
        /// Декодирование одного символа с помощью данного дерева Хаффмана
        /// </summary>
        /// <param name="code"></param>
        /// <param name="huffmanTree"></param>
        /// <returns></returns>
        private static char DecodeSymbol(ref string code, BinaryTree<string> huffmanTree)
        {
            var node = huffmanTree.Root;
            var count = 0;
            foreach (var i in code)
            {
                if (node.Value.Length == 1)
                {
                    code = code.Remove(0, count);
                    return node.Value.ElementAt(0);
                }

                if (i == '0')
                {
                    node = node.LeftChild;
                    count++;
                }
                else
                {
                    node = node.RightChild;
                    count++;
                }
            }

            //На случай, если файл отсутсвует
            throw new FileNotFoundException();
        }

        /// <summary>
        /// Преобразовывает строку
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static byte[] StringToByte(string str)
        {
            var bytes = new byte[str.Length / 8];
            for (var i = 0; i < str.Length / 8; i++)
                bytes[i] = Convert.ToByte(str.Substring(i * 8, 8), 2);
            return bytes;
        }
    }
}