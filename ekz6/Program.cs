using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver;
using System.Linq;
using System.Runtime;
using System.Net;

namespace dotnet
{


    public static class GraphDB
    {
        public class Page
        {
            public string UniqName { get; }
            public string Title { get; private set; }
            public string Author { get; }
            public PageType Type { get; }
            public string Source { get; }
            public string Path { get; }
            public DateTime CreationTime { get; }
            public string Labels { get; }
            public string EditingBy { get; private set; }
            public Page(INode node)
            {
                var nodeProperties = node.Properties.ToDictionary(prop => prop.Key, prop => prop.Value);
                UniqName = nodeProperties["uniqName"].As<string>();
                Title = nodeProperties["title"].As<string>();
                Author = nodeProperties["author"].As<string>();
                Source = nodeProperties["source"].As<string>();
                CreationTime = nodeProperties["creationTime"].As<string>().ToDateTime();
                Path = nodeProperties["path"].As<string>();
                Labels = nodeProperties["labels"].As<string>();
                EditingBy = nodeProperties["editingBy"].As<string>();
                switch (nodeProperties["type"].As<string>())
                {
                    case "TextDocument":
                        Type = PageType.TextDocument;
                        break;
                    case "ExcelDocument":
                        Type = PageType.ExcelDocument;
                        break;
                    case "Image":
                        Type = PageType.Image;
                        break;
                    case "WebPage":
                        Type = PageType.WebPage;
                        break;
                    case "MediaFile":
                        Type = PageType.MediaFile;
                        break;
                    case "OtherFile":
                        Type = PageType.OtherFile;
                        break;
                }

            }
            public class PageReadOnlyException : Neo4jException
            {
                //Конструктор
                public PageReadOnlyException(string message) : base(message) { }
            }
            //Класс исключений "Страница уже редактируется текущим пользователем"
            public class PageAlreadyEditingByCurrentUserException : Neo4jException
            {
                //Конструктор
                public PageAlreadyEditingByCurrentUserException(string message) : base(message) { }
            }
            //Метод возвращает каталоги, в которых находится текущая страница
            //Вход: session -сессия.
            public async Task<List<Catalog>> GetParentCatalogs(Neo4j.Driver.IAsyncSession session)
            {
                var result = await session.RunAsync("MATCH (c:Catalog)-[:CONTAINS]->(p:Page) WHERE p.uniqName='" + this.UniqName + "' RETURN c");
                List<INode> list = await result.ToListAsync<INode>(record => record[0].As<INode>());
                List<Catalog> catalogs = new List<Catalog>();
                if (list.Count > 0)
                {
                    foreach (INode node in list)
                        catalogs.Add(new Catalog(node));
                }
                return catalogs;
            }
            //Метод переименовывает страницу
            //Вход: session -сессия; newTitle -новоеимястраницы
            public void Rename(Neo4j.Driver.IAsyncSession session, string newTitle)
            {
                session.RunAsync("MATCH (p:Page) WHERE p.uniqName='" + this.UniqName + "' SET p.title='" + newTitle + "';");
                this.Title = newTitle;
            }
            //Метод делает файл read only для всех пользователей, кроме указанного
            public void BeginEditBy(Neo4j.Driver.IAsyncSession session, string username)
            {
                if (this.EditingBy == "")
                {
                    session.RunAsync("MATCH (p:Page) WHERE p.uniqName='" + this.UniqName + "' SET p.editingBy='" + username + "';");
                    EditingBy = username;
                }
                else if (this.EditingBy == username)
                    throw new PageAlreadyEditingByCurrentUserException("Выужередактируетеданнуюстраницу!");
                else
                    throw new PageReadOnlyException("Страница уже редактируется другим пользователем!");
            }
            //Метод завершает редактирование файла
            public void EndEditBy(Neo4j.Driver.IAsyncSession session, string username)
            {
                if (this.EditingBy == username)
                {
                    session.RunAsync("MATCH (p:Page) WHERE p.uniqName='" + this.UniqName + "' SET p.editingBy='';");
                    EditingBy = "";
                }
                else
                    throw new PageReadOnlyException("Страница уже редактируется другим пользователем!");
            }
        }
        public class Catalog
        {
            public string UniqName { get; } //уникальное имя каталога
            public string Title { get; private set; } //Название каталога
            public string Author { get; } //Автор(создатель) каталога
            public DateTime CreationTime { get; } //Время создания каталога
                                                  //КОНСТРУКТОРЫ
            public Catalog(string name, string title, string author)
            {
                UniqName = name; Title = title; Author = author; CreationTime = DateTime.Now;
            }
            public Catalog(INode node)
            {
                var nodeProperties = node.Properties.ToDictionary(prop => prop.Key, prop => prop.Value);
                UniqName = nodeProperties["uniqName"].As<string>();
                Title = nodeProperties["title"].As<string>();
                Author = nodeProperties["author"].As<string>();
                CreationTime = nodeProperties["creationTime"].As<string>().ToDateTime();
            }
            //МЕТОДЫ
            //Метод возвращает список страниц, лежащих в указанном каталоге.
            //Вход: session -сессия.
            //Выход: pages -список страниц.
            public async Task<List<Page>> GetPages(Neo4j.Driver.IAsyncSession session)
            {
                var result = await session.RunAsync("MATCH (c:Catalog)-[:CONTAINS]->(p:Page) WHERE c.uniqName='" + this.UniqName + "' RETURN p");
                List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
                List<Page> pages = new List<Page>();
                foreach (INode node in list)
                    pages.Add(new Page(node));
                return pages;
            }
            //Метод возвращает список подкаталогов, лежащих в указанном каталоге.
            //Вход: session -сессия.
            //Выход: pages -список подкаталогов.
            public async Task<List<Catalog>> GetSubcatalogs(Neo4j.Driver.IAsyncSession session)
            {
                var result = await session.RunAsync("MATCH (c:Catalog)-[:CONTAINS]->(sc:Catalog) WHERE c.uniqName='" + this.UniqName + "' RETURN sc");
                List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
                List<Catalog> pages = new List<Catalog>();
                foreach (INode node in list)
                    pages.Add(new Catalog(node));
                return pages;
            }
            //Метод возвращает каталоги, в которых находится текущая страница
            //Вход: session -сессия.
            public async Task<List<Catalog>> GetParentCatalogs(Neo4j.Driver.IAsyncSession session)
            {
                var result = await session.RunAsync("MATCH (pc:Catalog)-[:CONTAINS]->(c:Catalog) WHERE c.uniqName='" + this.UniqName + "' RETURN pc");
                List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
                List<Catalog> catalogs = new List<Catalog>();
                if (list.Count > 0)
                {
                    foreach (INode node in list)
                        catalogs.Add(new Catalog(node));
                }
                return catalogs;
            }
            //Метод переименовывает каталог
            //Вход: session -сессия; newTitle -новоеимякаталога
            public void Rename(Neo4j.Driver.IAsyncSession session, string newTitle)
            {
                session.RunAsync("MATCH (c:Catalog) WHERE c.uniqName='" + this.UniqName + "' SET c.title='" + newTitle + "';");
                this.Title = newTitle;
            }
        }
        //Класс-МЕТКА
        public class Label
        {
            public string UniqName { get; }
            //КОНСТРУКТОР
            public Label(INode node)
            {
                var nodeProperties = node.Properties.ToDictionary(prop => prop.Key, prop => prop.Value);
                UniqName = nodeProperties["uniqName"].As<string>();
            }
        }

        public class Relationship
        {
            public string UniqName { get; } //тип связи(уникальное имя отношения)
            public string Title { get; } //словесная интерпретация
            public List<string> StartNodeTypes { get; } //типы узлов, от которых может отходить связь
            public List<string> EndNodeTypes { get; } //типы узлов, к которым может подходить связь
                                                      //КОНСТРУКТОР
            public Relationship(INode node)
            {
                var nodeProperties = node.Properties.ToDictionary(prop => prop.Key, prop => prop.Value);
                UniqName = nodeProperties["uniqName"].As<string>();
                Title = nodeProperties["title"].As<string>();
                StartNodeTypes = nodeProperties["startNodeTypes"].As<List<string>>();
                EndNodeTypes = nodeProperties["endNodeTypes"].As<List<string>>();
            }
            //Класс исключений "Цикл в отношениях"
            public class RelationshipCycleException : Neo4jException
            {
                //Конструктор
                public RelationshipCycleException(string message) : base(message) { }
            }
            //Класс исключений "Отношение уже существует"
            public class RelationshipAlreadyExistException : Neo4jException
            {
                //Конструктор
                public RelationshipAlreadyExistException(string message) : base(message) { }
            }
        }
        public static Catalog ROOTCAT = new Catalog("ROOTCAT", "КОРНЕВОЙКАТАЛОГ", "neo4j");
        //Перечисление типов файлов, хранимых в системе
        public enum PageType
        {
            TextDocument = 1, //текстовыйдокумент
            ExcelDocument = 2, //таблица
            Image = 3, //изображение
            WebPage = 4, //веб-страница
            MediaFile = 5, //аудио-или видео-файл
            OtherFile = 6 //неизвестный тип файла
        }//Расшифровка типов файлов и типов узлов
        public static Dictionary<string, PageType> TextToPageType = new Dictionary<string, PageType>()
        {
        { "Текстовый документ", PageType.TextDocument },
        { "Таблица", PageType.ExcelDocument },
        { "Изображение", PageType.Image },
        { "Веб-страница", PageType.WebPage },
        { "Аудио-иливидеофайл" , PageType.MediaFile },
        { "Файл", PageType.OtherFile }
        };
        public static Dictionary<PageType, string> PageTypeToText = new Dictionary<PageType, string>()
        {
        { PageType.TextDocument, "Текстовыйдокумент" },
        { PageType.ExcelDocument, "Таблица" },
        { PageType.Image, "Изображение" },
        { PageType.WebPage, "Веб-страница" },
        { PageType.MediaFile, "Аудио-иливидеофайл" },
        { PageType.OtherFile, "Файл" }
        };
        public static Dictionary<string, string> NodeTypeToText = new Dictionary<string, string>()
        {
        { "Page", "Страница" },
        { "Catalog", "Каталог" },
        { "Label", "Метка" },
        { "Relationship", "Связь" }
        };
        public static Dictionary<string, string> TextToNodeType = new Dictionary<string, string>()
        {
        { "Страница", "Page" },
        { "Каталог", "Catalog" },
        { "Метка", "Label" },
        { "Связь", "Relationship" }
        };
        //Класс проверки на совпадение страниц (по их id)
        class PageEqualityComparer : IEqualityComparer<Page>
        {
            public bool Equals(Page p1, Page p2)
            {
                if (p2 == null && p1 == null) return true;
                else if (p1 == null || p2 == null) return false;
                else if (p1.UniqName == p2.UniqName) return true;
                else return false;
            }
            public int GetHashCode(Page p)
            {
                return p.UniqName.GetHashCode();
            }
        }
        //Фильтры запросов(для раздела where)
        public struct Filters
        {
            public List<PageType> checkedPageTypes; //выбранные типы страниц
            public List<string> authors; //авторы страниц/каталогов
                                         //Метод проверяет, совпадают ли наборы параметров
            public bool Equal(Filters anotherFilter)
            {
                //Проверяем совпадение типов страниц
                if (this.checkedPageTypes.Count == anotherFilter.checkedPageTypes.Count)
                {
                    for (int i = 0; i < this.checkedPageTypes.Count; i++)
                    {
                        if (this.checkedPageTypes[i] != anotherFilter.checkedPageTypes[i])
                            return false;
                    }
                }
                else
                    return false;
                //Проверяем совпадение авторов
                if (this.authors.Count == anotherFilter.authors.Count)
                {
                    for (int i = 0; i < this.authors.Count; i++)
                    {
                        if (this.authors[i] != anotherFilter.authors[i])
                            return false;
                    }
                }
                else
                    return false;
                return true;
            }
        }
        //Параметры фильтра поиска по умолчанию
        public static Filters DefaultFilter()
        {
            Filters filter = new Filters();
            filter.checkedPageTypes = PageTypeToText.Keys.ToList();
            filter.authors = new List<string>();
            return filter;
        }
        //Режимы поиска
        public struct SearchModes
        {
            public bool showPagesLinkedByLabel; //флаг того, нужно ли добавить в результаты поиска страницы, связанные с найденными по метке
            public bool searchInSubcatalogs; //флаг того, что нужно искать в подкаталогах
                                             //Метод проверяет, совпадают ли наборы параметров
            public bool Equal(SearchModes anotherMode)
            {
                //Проверяем совпадение флага showPagesLinkedByLabel
                if (this.showPagesLinkedByLabel != anotherMode.showPagesLinkedByLabel)
                    return false;
                if (this.searchInSubcatalogs != anotherMode.searchInSubcatalogs)
                    return false;
                return true;
            }
        }
        //Режим поиска по умолчанию
        public static SearchModes DefaultSearchMode()
        {
            SearchModes mode = new SearchModes();
            mode.showPagesLinkedByLabel = false;
            mode.searchInSubcatalogs = false;
            return mode;
        }
        //Параметры расширенного поиска
        public struct SearchParam
        {
            public Filters Filters; //фильтры
            public SearchModes SearchModes; //режимы поиска
                                            //Метод проверяет, совпадают ли наборы параметров
            public bool Equal(SearchParam param)
            {
                //Проверяем совпадение фильтров
                if (!this.Filters.Equal(param.Filters))
                    return false;
                //Проверяем совпадение режимов поиска
                if (!this.SearchModes.Equal(param.SearchModes))
                    return false;
                return true;
            }
        }
        //Связанная вершина с типом связи
        public struct RelationshipTypeAndNodePair
        {
            public Relationship Relationship; //отношение
            public INode RelNode; //связанная вершина
                                  //КОНСТРУКТОР
            public RelationshipTypeAndNodePair(Relationship relationship, INode node)
            {
                Relationship = relationship; RelNode = node;
            }
        }
        //Метод задаёт начальные параметры расширенного поиска
        public static SearchParam MakeStartParams()
        {
            SearchParam startParams = new SearchParam();
            startParams.Filters = DefaultFilter();//фильтр по умолчанию
            startParams.SearchModes = DefaultSearchMode(); //режимы поиска по умолчанию
            return startParams;
        }
        //Расширение класса string для преобразования строки к DateTime
        public static DateTime ToDateTime(this string timeStr)
        {
            int year = int.Parse(timeStr.Substring(0, 4));
            int month = int.Parse(timeStr.Substring(5, 2));
            int day = int.Parse(timeStr.Substring(8, 2));
            int hours = int.Parse(timeStr.Substring(11, 2));
            int minutes = int.Parse(timeStr.Substring(14, 2));
            int seconds = int.Parse(timeStr.Substring(17, 2));
            int milliseconds = int.Parse(timeStr.Substring(20, 3));
            return new DateTime(year, month, day, hours, minutes, seconds, milliseconds);
        }
        //Расширение списка страниц для проверки совпадения страниц (их уникальных имён)
        public static bool Contain(this List<Page> thisList, Page page)
        {
            if (thisList.Select(p => p.UniqName).ToList().Contains(page.UniqName))
                return true;
            else
                return false;
        }
        //Метод добавляет в базу данных новый каталог
        //Вход: session -сессия; catalogName-уникальное имя (ID) каталога; catalogTitle -название каталога.
        public static void AddCatalog(IAsyncSession session, string catalogName, string catalogTitle, string catalogAuthor)
        {
            session.RunAsync("CREATE (a:Catalog {uniqName:'" + catalogName + "', title:'" + catalogTitle + "', author:'" + catalogAuthor + "', creationTime:datetime()})");
        }
        //Метод добавляет в базу данных новую страницу
        //Вход: session -сессия; pageName -уникальное имя (ID) страницы; pageTitle -название страницы; pageAuthor -автор страницы (пользователь-создатель); filePath -путь до файла с данными страницы.
        public static void AddPage(IAsyncSession session, string pageName, string pageTitle, PageType pageType, string pageAuthor, string pageSource, string filePath, string pageLabels)
        {
            //Проверяем список меток: если какой-то из перечисленных меток ещё не существует, то создаём её
            session.RunAsync("CREATE (a:Page {uniqName:'" + pageName + "', title:'" + pageTitle + "', type:'" + pageType.ToString() + "', author:'" + pageAuthor + "', source:'" + pageSource + "', path:'" + filePath.Replace('\\', '/') + "', creationTime:datetime(), labels:'" + pageLabels + "', editingBy:'' })");
        }
        //Метод добавляет в базу данных информацию о новом типе связи
        //Вход: session -сессия; ...
        public static void CreateRelationshipType(Neo4j.Driver.IAsyncSession session, string relUniqName, string relTitle, List<string> startNodeType, List<string> endNodeType)
        {
            //Составляем строку со списком типов узлов, от которых может отходить связь
            string startNodes = "[";
            for (int i = 0; i < startNodeType.Count - 1; i++)
                startNodes += "'" + startNodeType[i] + "', ";
            startNodes += "'" + startNodeType[startNodeType.Count - 1] + "']";
            //Составляем строку со списком типов узлов, к которым может подходить связь
            string endNodes = "[";
            for (int i = 0; i < endNodeType.Count - 1; i++)
                endNodes += "'" + endNodeType[i] + "', ";
            endNodes += "'" + endNodeType[endNodeType.Count - 1] + "']";
            session.RunAsync("CREATE (rel:Relationship {uniqName:'" + relUniqName + "', title:'" + relTitle + "', startNodeTypes:" + startNodes + ", endNodeTypes:" + endNodes + "});");
        }
        //Метод проверяет список меток: если какой-то из перечисленных меток ещё не существует, то создаёт её.
        //Вход: session -сессия; labels -список меток через запятую.
        private static async void CheckLabels(Neo4j.Driver.IAsyncSession session, string labelsStr)
        {
            //Получим список всех существующих меток
            IResultCursor result = await session.RunAsync("MATCH l:Label return l;");
            try
            {
                List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
                List<string> labels = new List<string>();
                if (list.Count != 0)
                {
                    foreach (INode n in list)
                        labels.Add(new Label(n).UniqName);
                }
                //Определим, какие из меток, перечисленные в labelsStr, отсутствуют в базе данных и добавим их туда
                string[] pageLabels = labelsStr.Split(',');
                //массив меток страницы
                foreach (string lab in pageLabels)
                {
                    string label = lab.Trim().ToLower();
                    if (!labels.Contains(label))
                    {
                        //Добавляем новую метку в базу данных
                        session.RunAsync("CREATE (l:Label { uniqName:'" + label + "' });");
                    }
                }
            }
            catch (Exception ex)
            { }
            finally { };
        }
        //Метод добавляет связь между двумя указанными страницами//Вход: session -сессия; pageName1 -idпервой страницы; pageName2 -id второй страницы; relType -тип связи.
        public static void AddRelationship(Neo4j.Driver.IAsyncSession session, string nodeName1, string nodeType1, string nodeName2, string nodeType2, string relType)
        {
            if (!CheckAlreadyExistRelationship(session, nodeName1, nodeType1, nodeName2, nodeType2, relType).Result)
            {
                if (!CheckCycle(session, nodeName1, nodeType1, nodeName2, nodeType2, relType).Result)
                {
                    session.RunAsync("MATCH (n1:" + nodeType1 + "{ uniqName:'" + nodeName1 + "'}) OPTIONAL MATCH (n2:" + nodeType2 + "{ uniqName:'" + nodeName2 + "'}) CREATE (n1)-[:" + relType + "]->(n2);");
                }
                else
                    throw new Relationship.RelationshipCycleException("Нельзя добавить связь между данными объектами, так как это создаст цикл!");
            }
            else throw new Relationship.RelationshipAlreadyExistException("Данный тип связи уже существует между этими объектами!");
        }
        //Метод проверяет, появится ли цикл, если добавить данную связь между двумя указанными вершинами//Вход: session -сессия; fromNodeName1 -id вершины, от которой хотим добавить новую связь; toNodeName2 -id вершины, к которой хотим подвести новую связь; relType -тип связи.
        private static async Task<bool> CheckCycle(Neo4j.Driver.IAsyncSession session, string fromNodeName, string fromNodeType, string toNodeName, string toNodeType, string relType)
        {
            //Проверяем наличие пути между toNodeName и fromNodeType по заданному типу связи
            var result = await session.RunAsync("match path = (n1:" + toNodeType + " { uniqName:'" + toNodeName + "'})-[:" + relType + "*]->(n2:" + fromNodeType + " { uniqName:'" + fromNodeName + "'}) return length(path);");
            List<int> list = await result.ToListAsync(record => (record[0].As<int>()));
            if (list != null && list.Count > 0)
                return true; //если путь есть => появится цикл при добавлении связи
            else return false; //если пути нет => цикл не появится
        }
        //Метод проверяет, существует ли уже между двумя заданными узлами указанная связь
        //Вход: session -сессия; fromNodeName1 -id вершины, от которой хотим добавить новую связь; toNodeName2 -id вершины, к которой хотим подвести новую связь; relType -тип связи.
        private static async Task<bool> CheckAlreadyExistRelationship(Neo4j.Driver.IAsyncSession session, string fromNodeName, string fromNodeType, string toNodeName, string toNodeType, string relType)
        {
            //Проверяем наличие связи между toNodeName и fromNodeType по заданному типу связи
            var result = await session.RunAsync("match (n1:" + fromNodeType + " { uniqName:'" + fromNodeName + "'})-[r:" + relType + "]->(n2:" + toNodeType + " { uniqName:'" + toNodeName + "'}) return r;");
            List<int> list = await result.ToListAsync(record => (record[0].As<int>()));
            if (list.Count > 0)
                return true; //если связь есть
            else
                return false; //если связи нет
        }
        //Метод добавляет страницу в каталог//Вход: session -сессия; catalogName -уникальное имя (ID) каталога; pageName-уникальное имя (ID) страницы.
        public static void AddPageToCatalog(Neo4j.Driver.IAsyncSession session, string catalogName, string pageName)
        {
            if (!CheckCycle(session, catalogName, "Catalog", pageName, "Page", "CONTAINS").Result)
            {
                session.RunAsync("MATCH (a:Catalog), (b:Page) WHERE a.uniqName='" + catalogName + "' AND b.uniqName='" + pageName + "' MERGE ((a)-[:CONTAINS]->(b))");
            }
            else
                throw new Relationship.RelationshipCycleException("Нельзя добавить связь между данными объектами, так как это создаст цикл!");
        }
        //Метод добавляет подкаталог в каталог//Вход: session -сессия; catalogName -уникальное имя (ID) каталога; subCatName -уникальное имя (ID) каталога.
        public static void AddSubcatalogToCatalog(Neo4j.Driver.IAsyncSession session, string catalogName, string subCatalogName)
        {
            session.RunAsync("MATCH (a:Catalog), (b:Catalog) WHERE a.uniqName='" + catalogName + "' AND b.uniqName='" + subCatalogName + "' MERGE ((a)-[:CONTAINS]->(b))");
        }
        //Метод удаляет страницу из базы данных (вместе со всеми связями)
        //Вход: session -сессия; uniqName -уникальное имя (ID) страницы
        public static void DeletePage(Neo4j.Driver.IAsyncSession session, string pageName, string ftpServer)
        {
            Page page = GetPage(session, pageName).Result;
            if (page.EditingBy == "")
            {
                session.RunAsync("MATCH (p:Page) WHERE p.uniqName='" + pageName + "' DETACH DELETE p");//удаляет метаданные
                                                                                                       //Удаляем файл страницы с ftp сервера
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpServer + "/" + page.Path);
                request.Credentials = new NetworkCredential("fileloader", "password");
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                response.Close();
            }
            else
                throw new Page.PageReadOnlyException("Страница редактируется вами или другим пользователем!\n Повторите попытку позже.");
        }
        //Метод удаляет каталог из базы данных (вместе со всеми связями)
        //Вход: session -сессия; catalogName -уникальное имя (ID) каталога
        public static void DeleteCatalog(Neo4j.Driver.IAsyncSession session, string catalogName)
        {
            session.RunAsync("MATCH (c:Catalog) WHERE c.uniqName='" + catalogName + "' DETACH DELETE c");
        }
        //Метод удаляет связь между двумя указанными узлами
        //Вход: session -сессия; fromNodeName1 -idвершины, от которой отходит связь; toNodeName2 -id вершины, к которой подходит связь; relType -тип связи.
        public static void DeleteRelationship(Neo4j.Driver.IAsyncSession session, string fromNodeName, string fromNodeType, string toNodeName, string toNodeType, string relType)
        {
            session.RunAsync("match (n1:" + fromNodeType + " { uniqName:'" + fromNodeName + "'})-[rel:" + relType + "]-(n2:" + toNodeType + " { uniqName:'" + toNodeName + "'}) delete rel;");
        }
        //Метод убирает страницу из каталога (сама страница сохраняется)
        //Вход: session -сессия; catalogName -уникальное имя (ID) каталога; pageName -уникальное имя (ID) страницы.
        public static void RemovePageFromCatalog(Neo4j.Driver.IAsyncSession session, string catalogName, string pageName)
        {
            session.RunAsync("MATCH (C:Catalog {uniqName:'" + catalogName + "'})-[r:CONTAINS]->(p:Page {uniqName:'" + pageName + "'}) DELETE r");
        }
        //Метод убирает подкаталог из каталога (сам подкаталог сохраняется)
        //Вход: session -сессия; parentCatalogName -уникальное имя (ID) каталога; childCatalogName -уникальное имя (ID) подкаталога.
        public static void RemoveSubcatalogFromCatalog(Neo4j.Driver.IAsyncSession session, string parentCatalogName, string childCatalogName)
        {
            session.RunAsync("MATCH (pc:Catalog {uniqName:'" + parentCatalogName + "'})-[r:CONTAINS]->(cc:Catalog {uniqName:'" + childCatalogName + "'}) DELETE r");
        }
        //Метод возвращает каталог с указанным именем
        //Вход: session -сессия; catalogName -уникальное имя (ID) каталога.
        //Выход: каталогсуказаннымименем
        public static async Task<Catalog> GetCatalog(Neo4j.Driver.IAsyncSession session, string catalogName)
        {
            var result = await session.RunAsync("MATCH (c:Catalog) WHERE c.uniqName='" + catalogName + "' RETURN c");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            if (list.Count == 0)
                return null;
            else
                return new Catalog(list[0]);
        }
        //Метод возвращает страницу с указанным именем
        //Вход: session -сессия; pageName-уникальное имя (ID) страницы.
        //Выход: страницасуказаннымименем
        public static async Task<Page> GetPage(Neo4j.Driver.IAsyncSession session, string pageName)
        {
            var result = await session.RunAsync("MATCH (p:Page) WHERE p.uniqName='" + pageName + "' RETURN p");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            if (list.Count == 0)
                return null;
            else
                return new Page(list[0]);
        }
        //Метод возвращает тип отношения (связи) с указанным именем
        //Вход: session-сессия; relName -уникальное имя (ID) типа отношения (связи).
        //Выход: тип отношения (связи) с указанным именем
        public static async Task<Relationship> GetRelationship(Neo4j.Driver.IAsyncSession session, string relName)
        {
            var result = await session.RunAsync("MATCH (r:Relationship) WHERE r.uniqName='" + relName + "' RETURN r");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            if (list.Count == 0)
                return null;
            else
                return new Relationship(list[0]);
        }
        //Метод находит все связи, выходящие из указанного узла, и связанные узлы.
        //Вход: session -сессия; nodeName -id узла; nodeType -тип узла.
        //Выход: findRels-связи, выходящие из данной вершины, со связанными вершинами.
        public static async Task<List<RelationshipTypeAndNodePair>> GetExitingNodeRelationships(Neo4j.Driver.IAsyncSession session, string nodeName, string nodeType)
        {
            var result = await session.RunAsync("match (p:" + nodeType + " { uniqName:'" + nodeName + "'})-[r]->(n) with r, n optional match (rel:Relationship) where rel.uniqName=type(r) return rel.uniqName as relName, n as node;");
            List<IRecord> list = await result.ToListAsync(rec => rec.As<IRecord>());
            List<RelationshipTypeAndNodePair> findRels = new List<RelationshipTypeAndNodePair>();
            foreach (var rec in list)
            {
                findRels.Add(new RelationshipTypeAndNodePair(GetRelationship(session, rec.Values.Values.ToList()[0].ToString()).Result, rec.Values.Values.ToList()[1].As<INode>()));
            }
            return findRels;
        }
        //Метод находит все связи, входящие в указанный узел, и связанные узлы.
        //Вход: session -сессия; nodeName -id узла; nodeType -тип узла.
        //Выход: findRels -связи, входящие в указанный узел, со связанными узлами.
        public static async Task<List<RelationshipTypeAndNodePair>> GetIncomingNodeRelationships(Neo4j.Driver.IAsyncSession session, string nodeName, string nodeType)
        {
            var result = await session.RunAsync("match (p:" + nodeType + " { uniqName:'" + nodeName + "'})<-[r]-(n) with r, n optional match (rel:Relationship) where rel.uniqName=type(r) return rel.uniqName as relName, n as node;");
            List<IRecord> list = await result.ToListAsync(rec => rec.As<IRecord>());
            List<RelationshipTypeAndNodePair> findRels = new List<RelationshipTypeAndNodePair>();
            foreach (var rec in list)
            {
                findRels.Add(new RelationshipTypeAndNodePair(GetRelationship(session, rec.Values.Values.ToList()[0].ToString()).Result, rec.Values.Values.ToList()[1].As<INode>()));
            }
            return findRels;
        }
        //Метод возвращает страницу(ы) с указанным названием
        //Вход: session -сессия; pageTitle -название страницы.
        //Выход: pages-список страниц с указанным названием
        public static async Task<List<Page>> GetPagesByTitle(Neo4j.Driver.IAsyncSession session, string pageTitle)
        {
            //MATCH (p:Page) where p.title=~'.*Блок.*' return p.title;
            var result = await session.RunAsync("MATCH (p:Page) WHERE toLower(p.title)=~'" + pageTitle.ToLower() + ".*' RETURN p");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            List<Page> pages = new List<Page>();
            if (list.Count != 0)
            {
                foreach (INode p in list) pages.Add(new Page(p));
            }
            return pages;
        }
        //Метод возвращает все страницы, хранимые в базе данных
        //Вход: session -сессия.
        //Выход: pages -список страниц.
        public static async Task<List<Page>> GetAllPages(Neo4j.Driver.IAsyncSession session)
        {
            var result = await session.RunAsync("MATCH (p:Page) RETURN p");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            List<Page> pages = new List<Page>();
            if (list.Count != 0)
            {
                foreach (INode p in list) pages.Add(new Page(p));
            }
            return pages;
        }
        //Метод возвращает каталог(и) с указанным названием
        //Вход: session -сессия; catalogTitle -название каталога.
        //Выход: catalogs-список каталогов с указанным названием
        public static async Task<List<Catalog>> GetCatalogsByTitle(Neo4j.Driver.IAsyncSession session, string catalogTitle)
        {
            var result = await session.RunAsync("MATCH (c:Catalog) WHERE toLower(c.title)=~'" + catalogTitle.ToLower() + ".*' RETURN c");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            List<Catalog> catalogs = new List<Catalog>();
            if (list.Count != 0)
            {
                foreach (INode c in list) catalogs.Add(new Catalog(c));
            }
            return catalogs;
        }
        //Метод возвращает все каталоги, хранимые в базе данных
        //Вход: session -сессия.
        //Выход: catalogs -список каталогов
        public static async Task<List<Catalog>> GetAllCatalogs(Neo4j.Driver.IAsyncSession session)
        {
            var result = await session.RunAsync("MATCH (c:Catalog) RETURN c");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            List<Catalog> catalogs = new List<Catalog>();
            if (list.Count != 0)
            {
                foreach (INode c in list) catalogs.Add(new Catalog(c));
            }
            return catalogs;
        }
        //Метод возвращает все используемые в данный момент в базе данных типы отношений (связей)
        //Вход: session -сессия
        //Выход: list -список типов отношений (связей)
        public static async Task<List<string>> GetUsedRelationshipTypes(Neo4j.Driver.IAsyncSession session)
        {
            var result = await session.RunAsync("CALL db.schema.relTypeProperties() YIELD relType RETURN DISTINCT relType;");
            List<string> list = await result.ToListAsync(record => (record[0].As<string>()).Substring(1).Trim('`'));
            return list;
        }
        //Метод возвращает все допустимые типы отношений (связей)
        //Вход: session -сессия
        //Выход: list -список типов отношений (связей)
        public static async Task<List<Relationship>> GetAllRelationshipTypes(Neo4j.Driver.IAsyncSession session)
        {
            var result = await session.RunAsync("MATCH (rel:Relationship) RETURN rel;");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            List<Relationship> rels = new List<Relationship>();
            foreach (var node in list)
                rels.Add(new Relationship(node));
            return rels;
        }
        //Метод возвращает все метки
        //Вход: session -сессия
        //Выход: list -список меток
        public static async Task<List<Label>> GetAllLabels(Neo4j.Driver.IAsyncSession session)
        {
            var result = await session.RunAsync("MATCH (l:Label) return l;");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            List<Label> labels = new List<Label>();
            if (list.Count != 0)
            {
                foreach (INode l in list)
                    labels.Add(new Label(l));
            }
            return labels;
        }
        //Метод выполняет полнотекстовый поиск страниц по названию.
        //Вход: session -сессия; searchStr -строка поиска (поисковый запрос).
        //Выход: pages -список найденных страниц.
        public static async Task<List<Page>> FulltextPageQuery(Neo4j.Driver.IAsyncSession session, string searchStr, SearchParam param)
        {

            IResultCursor result;
            string fullRequest;
            string whereStr = "";//строка фильтрации найденных страниц
                                 //Фильтрация
            if (!param.Filters.Equal(DefaultFilter()))
            {
                whereStr = "WHERE ";
                if (param.Filters.checkedPageTypes.Count == 0)
                    return new List<Page>(); //если не выбран ни один из типов страниц
                else if (param.Filters.checkedPageTypes.Count < PageTypeToText.Count)
                {
                    whereStr += "node.type in([";
                    for (int i = 0; i < param.Filters.checkedPageTypes.Count - 1; i++)
                        whereStr += ("'" + param.Filters.checkedPageTypes[i].ToString() + "',");
                    whereStr += "'" + param.Filters.checkedPageTypes[param.Filters.checkedPageTypes.Count - 1].ToString() + "'])";
                }
            }
            //Основной запрос
            fullRequest = "CALL db.index.fulltext.queryNodes('pageTitles', '" + searchStr + "') YIELD node " + whereStr + " RETURN 1 as num, node as page";
            //Дополнительные режимы поиска
            if (param.Filters.Equal(DefaultFilter()) && !param.SearchModes.Equal(DefaultSearchMode()))
            {
                //Поиск по связям меток
                if (param.SearchModes.showPagesLinkedByLabel == true)
                {
                    fullRequest += " union CALL db.index.fulltext.queryNodes('pageTitles', '" + searchStr + "') YIELD node with node match (l:Label) where l.uniqName in(split(trim(node.labels), ',')) with l match (l)-[]-(lab:Label) with lab match (p:Page) where lab.uniqName in(split(trim(p.labels), ',')) return 2 as num, p as page";
                }
            }
            result = await session.RunAsync(fullRequest);
            var list = await result.ToListAsync(record => record.As<IRecord>());
            List<Page> pages = new List<Page>();
            foreach (var p in list)
            {
                pages.Add(new Page(p.Values.Values.ToList()[1].As<INode>()));
            }
            //Удаляем дубликаты
            PageEqualityComparer comparer = new PageEqualityComparer();
            pages = pages.Distinct(comparer).ToList();
            return pages;
        }
        //Метод выполняет полнотекстовый поиск страниц по названию в указанном каталоге.
        //Вход: session -сессия; catalogName -уникальный id каталога, в котором нужно искать; searchStr -строка поиска (поисковый запрос); 
        //Выход: pages -список найденных страниц.
        public static async Task<List<Page>> FulltextPageQuery(Neo4j.Driver.IAsyncSession session, string catalogName, string searchStr, SearchParam param)
        {
            IResultCursor result;
            string fullRequest;
            string whereStr = ""; //строка фильтрации найденных страниц
            string rel = "";
            //Если нужно искать в подкаталогах
            if (param.SearchModes.searchInSubcatalogs) rel = "*";
            //Фильтрация
            if (!param.Filters.Equal(DefaultFilter()))
            {
                whereStr = "and ";
                if (param.Filters.checkedPageTypes.Count == 0)
                    return new List<Page>();//если не выбран ни один из типов страниц
                else if (param.Filters.checkedPageTypes.Count < PageTypeToText.Count)
                {
                    whereStr += "node.type in([";
                    for (int i = 0; i < param.Filters.checkedPageTypes.Count - 1; i++)
                        whereStr += ("'" + param.Filters.checkedPageTypes[i].ToString() + "',");
                    whereStr += "'" + param.Filters.checkedPageTypes[param.Filters.checkedPageTypes.Count - 1].ToString() + "'])";
                }
            }
            //Основной запрос
            fullRequest = "CALL db.index.fulltext.queryNodes('pageTitles', '" + searchStr + "') YIELD node WITH node MATCH (cat:Catalog)-[" + rel + "]->(n:Page) where cat.uniqName='" + catalogName + "' and n=node " + whereStr + " RETURN 1 as num, node as page";
            //Дополнительные режимы поиска
            if (param.Filters.Equal(DefaultFilter()) && !param.SearchModes.Equal(DefaultSearchMode()))
            {
                if (param.SearchModes.showPagesLinkedByLabel == true)
                {
                    fullRequest += " union CALL db.index.fulltext.queryNodes('pageTitles', '" + searchStr + "') YIELD node with node match (l:Label) where l.uniqName in(split(trim(node.labels), ',')) with l match (l)-[]-(lab:Label) with lab match (p:Page) where lab.uniqName in(split(trim(p.labels), ',')) return 2 as num, p as page";
                }
            }
            result = await session.RunAsync(fullRequest);
            var list = await result.ToListAsync(record => record.As<IRecord>());
            List<Page> pages = new List<Page>();
            foreach (var p in list)
            {
                pages.Add(new Page(p.Values.Values.ToList()[1].As<INode>()));
            }
            //Удаляем дубликаты
            PageEqualityComparer comparer = new PageEqualityComparer();
            pages = pages.Distinct(comparer).ToList();
            return pages;
        }
        //Метод выполняет полнотекстовый поиск страниц по меткам.
        //Вход: session -сессия; searchStr -строка поиска (поисковый запрос).
        //Выход: pages -список найденных страниц.
        public static async Task<List<Page>> FulltextPageLabelQuery(Neo4j.Driver.IAsyncSession session, string searchStr, SearchParam param)
        {
            //Разбиваем список меток на отдельные метки
            string[] labels = searchStr.Split(','); //массив искомых меток
            List<Page> pages = new List<Page>(); //список найденных страниц -точное совпадение
            List<Page> associatedPages = new List<Page>();//список ассоциативных страниц
            foreach (string label in labels)
            {
                IResultCursor result;
                string fullRequest;
                string whereStr = ""; //строка фильтрации найденных страниц
                                      //Фильтрация
                if (!param.Filters.Equal(DefaultFilter()))
                {
                    whereStr = "WHERE ";
                    if (param.Filters.checkedPageTypes.Count == 0)
                        return new List<Page>(); //если не выбран ни один из типов страниц
                    else if (param.Filters.checkedPageTypes.Count < PageTypeToText.Count)
                    {
                        whereStr += "node.type in([";
                        for (int i = 0; i < param.Filters.checkedPageTypes.Count - 1; i++)
                            whereStr += ("'" + param.Filters.checkedPageTypes[i].ToString() + "',");
                        whereStr += "'" + param.Filters.checkedPageTypes[param.Filters.checkedPageTypes.Count - 1].ToString() + "'])";
                    }
                }
                //Основной запрос
                fullRequest = "CALL db.index.fulltext.queryNodes('pageLabels', '" + label.Trim().ToLower() + "') YIELD node " + whereStr + " RETURN 1 as num, node as page";
                //Дополнительные режимы поиска
                if (param.Filters.Equal(DefaultFilter()) && !param.SearchModes.Equal(DefaultSearchMode()))
                {
                    if (param.SearchModes.showPagesLinkedByLabel == true)
                    {
                        fullRequest += " union CALL db.index.fulltext.queryNodes('pageLabels', '" + label.Trim().ToLower() + "') YIELD node with node match (l:Label) where l.uniqName in(split(trim(node.labels), ',')) with l match (l)-[]-(lab:Label) with lab match (p:Page) where lab.uniqName in(split(trim(p.labels), ',')) return 2 as num, p as page";
                    }
                }
                result = await session.RunAsync(fullRequest);
                var list = await result.ToListAsync(record => record.As<IRecord>());
                foreach (var p in list)
                {
                    pages.Add(new Page(p.Values.Values.ToList()[1].As<INode>()));
                }
            }
            //Удаляем дубликаты
            PageEqualityComparer comparer = new PageEqualityComparer();
            pages = pages.Distinct(comparer).ToList();
            return pages;
        }
        //Метод выполняет полнотекстовый поиск страниц по меткам в указанном каталоге.
        //Вход: session -сессия; catalogName -уникальный id каталога, в котором нужно искать; searchStr -строка поиска (поисковый запрос).
        //Выход: pages -список найденных страниц.
        public static async Task<List<Page>> FulltextPageLabelQuery(Neo4j.Driver.IAsyncSession session, string catalogName, string searchStr, SearchParam param)
        {
            //Разбиваем список меток на отдельные метки
            string[] labels = searchStr.Split(','); //массив искомых меток
            List<Page> pages = new List<Page>(); //список найденных страниц
            foreach (string label in labels)
            {
                IResultCursor result;
                string fullRequest;
                string whereStr = ""; //строка фильтрации найденных страниц
                string rel = "";
                //Если нужно искать в подкаталогах
                if (param.SearchModes.searchInSubcatalogs) rel = "*";
                //Фильтрация
                if (!param.Filters.Equal(DefaultFilter()))
                {
                    whereStr = "and ";
                    if (param.Filters.checkedPageTypes.Count == 0)
                        return new List<Page>(); //если не выбран ни один из типов страниц
                    else if (param.Filters.checkedPageTypes.Count < PageTypeToText.Count)
                    {
                        whereStr += "node.type in([";
                        for (int i = 0; i < param.Filters.checkedPageTypes.Count - 1; i++)
                            whereStr += ("'" + param.Filters.checkedPageTypes[i].ToString() + "',");
                        whereStr += "'" + param.Filters.checkedPageTypes[param.Filters.checkedPageTypes.Count - 1].ToString() + "'])";
                    }
                }
                //Основной запрос
                fullRequest = "CALL db.index.fulltext.queryNodes('pageLabels', '" + label.Trim().ToLower() + "') YIELD node WITH node MATCH (cat:Catalog)-[" + rel + "]->(n:Page) where cat.uniqName='" + catalogName + "' and n=node " + whereStr + " RETURN 1 as num, node as page";
                //Дополнительные режимы поиска
                if (param.Filters.Equal(DefaultFilter()) && !param.SearchModes.Equal(DefaultSearchMode()))
                {
                    if (param.SearchModes.showPagesLinkedByLabel == true)
                    {
                        fullRequest += " union CALL db.index.fulltext.queryNodes('pageLabels', '" + label.Trim().ToLower() + "') YIELD node with node match (l:Label) where l.uniqName in(split(trim(node.labels), ',')) with l match (l)-[]-(lab:Label) with lab match (p:Page) where lab.uniqName in(split(trim(p.labels), ',')) return 2 as num, p as page";
                    }
                }
                result = await session.RunAsync(fullRequest);
                var list = await result.ToListAsync(record => record.As<IRecord>());
                foreach (var p in list)
                {
                    pages.Add(new Page(p.Values.Values.ToList()[1].As<INode>()));
                }
            }
            //Удаляем дубликаты
            PageEqualityComparer comparer = new PageEqualityComparer();
            pages = pages.Distinct(comparer).ToList();
            return pages;
        }
        //Метод выполняет полнотекстовый поиск каталогов.
        //Вход: session -сессия; searchStr -строка поиска (поисковый запрос).
        //Выход: catalogs -список найденных каталогов.
        public static async Task<List<Catalog>> FulltextCatalogQuery(Neo4j.Driver.IAsyncSession session, string searchStr, SearchParam param)
        {
            var result = await session.RunAsync("CALL db.index.fulltext.queryNodes('catalogTitles', '" + searchStr + "') YIELD node RETURN node");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            List<Catalog> catalogs = new List<Catalog>();
            if (list.Count != 0)
            {
                foreach (INode c in list)
                    catalogs.Add(new Catalog(c));
            }
            return catalogs;
        }
        //Метод выполняет полнотекстовый поиск каталогов в указанном каталоге.
        //Вход: session -сессия; catalogName -уникальный id каталога, в котором нужно искать; searchStr -строка поиска (поисковый запрос).
        //Выход: catalogs -список найденных каталогов.
        public static async Task<List<Catalog>> FulltextCatalogQuery(Neo4j.Driver.IAsyncSession session, string catalogName, string searchStr, SearchParam param)
        {
            string rel = "";
            //Если нужно искать в подкаталогах
            if (param.SearchModes.searchInSubcatalogs) rel = "*";
            var result = await session.RunAsync("CALL db.index.fulltext.queryNodes('catalogTitles', '" + searchStr + "') YIELD node WITH node MATCH (cat:Catalog)-[" + rel + "]->(n:Catalog) where cat.uniqName='" + catalogName + "' and n=node RETURN node;");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            List<Catalog> catalogs = new List<Catalog>();
            if (list.Count != 0)
            {
                foreach (INode c in list) catalogs.Add(new Catalog(c));
            }
            return catalogs;
        }
        //Метод выполняет полнотекстовый поиск меток по имени.
        //Вход: session -сессия; searchStr -строка поиска (поисковый запрос).
        //Выход: labels -список найденных меток.
        public static async Task<List<Label>> FulltextLabelQuery(Neo4j.Driver.IAsyncSession session, string searchStr, SearchParam param)
        {
            IResultCursor result;
            if (param.Equal(MakeStartParams()))
            {
                result = await session.RunAsync("CALL db.index.fulltext.queryNodes('labelUniqName', '" + searchStr + "') YIELD node RETURN node");
            }
            else
            {
                result = await session.RunAsync("CALL db.index.fulltext.queryNodes('labelUniqName', '" + searchStr + "') YIELD node RETURN node");
            }
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            List<Label> labels = new List<Label>();
            if (list.Count != 0)
            {
                foreach (INode l in list)
                    labels.Add(new Label(l));
            }
            return labels;
        }
    }
    public class GraphDBA
    {
        //Состояния запущенной базы данных
        public enum DBState
        {
            Normal, //нормальное
            HasErrors, //возникла ошибка (например, недоступен корневой каталог)
            Empty //база данных пуста => нуждается в создании
        }
        //Класс исключений "Недостаточно прав"
        public class InsufficientPermissionException : Neo4jException
        {
            //Конструктор
            public InsufficientPermissionException(string message) : base(message)
            {

            }
        }
        //Метод проверяет, обладает ли данный пользователь правами администратора
        public static async Task<bool> HasAdminRole(Neo4j.Driver.IAsyncSession session, string username)
        {
            var result = await session.RunAsync("call dbms.security.listRolesForUser('" + username + "');");
            var list = await result.ToListAsync(record => record.As<IRecord>());
            foreach (var role in list)
            {
                if (role.Values.Values.ToList()[0].ToString() == "admin")
                    return true;
            }
            return false;
        }
        //Метод удаляет все узлы и связи базы данных
        public static async void DeleteDB(Neo4j.Driver.IAsyncSession session, string FTP_SERVER)
        {
            //Удаляем все индексы
            await session.RunAsync("call db.index.fulltext.drop('pageTitles')");
            await session.RunAsync("call db.index.fulltext.drop('pageLabels')");
            await session.RunAsync("call db.index.fulltext.drop('catalogTitles')");
            await session.RunAsync("call db.index.fulltext.drop('catalogLabels')");
            await session.RunAsync("call db.index.fulltext.drop('labelUniqName')");
            //Удаляем все страницы
            List<GraphDB.Page> pages = await GraphDB.GetAllPages(session);
            foreach (var p in pages)
                GraphDB.DeletePage(session, p.UniqName, FTP_SERVER);
            //Удаляем все оставшиеся узлы и отношения
            await session.RunAsync("MATCH (n) OPTIONAL MATCH (n)-[r]-() DELETE n, r");
        }
        //Метод создаёт новую базу данных
        public static void CreateDB(Neo4j.Driver.IAsyncSession session, string username)
        {
            //Проверяем, что пользователь обладает правами администратора
            if (!HasAdminRole(session, username).Result)
                throw new InsufficientPermissionException("Недостаточно прав для выполнения операции создания новой базы данных! Пожалуйста, войдите с правами администратора.");
            //1. Создаём корневой каталог
            GraphDB.AddCatalog(session, "ROOTCAT", "КОРНЕВОЙ КАТАЛОГ", username);
            //2. Создаём предопределённые связи (отношения)
            GraphDB.CreateRelationshipType(session, "CONTAINS", "СОДЕРЖИТ", new List<string>() { "Catalog" }, new List<string>() { "Catalog", "Page" });
            GraphDB.CreateRelationshipType(session, "RELATED_TO", "СВЯЗАН(А) С", new List<string>() { "Catalog", "Page" }, new List<string>() { "Catalog", "Page" });
            GraphDB.CreateRelationshipType(session, "RELATED_LABEL_FOR", "РОДСТВЕННАЯМЕТКАДЛЯ", new List<string>() { "Label" }, new List<string>() { "Label" });
            //3. Создаём ограничения
            session.RunAsync("CREATE CONSTRAINT ON (c:Catalog) ASSERT c.uniqName IS UNIQUE;");
            session.RunAsync("CREATE CONSTRAINT ON (p:Page) ASSERT p.uniqName IS UNIQUE;");
            session.RunAsync("CREATE CONSTRAINT ON (l:Label) ASSERT l.uniqName IS UNIQUE;");
            session.RunAsync("CREATE CONSTRAINT ON (r:Relationship) ASSERT r.uniqName IS UNIQUE;");
            //4. Создаём полнотекстовые индексы
            session.RunAsync("call db.index.fulltext.createNodeIndex('pageTitles', ['Page'], ['title'], { analyzer: 'russian', eventually_consistent: 'true' });");
            session.RunAsync("call db.index.fulltext.createNodeIndex('pageLabels', ['Page'], ['labels'], { analyzer: 'russian', eventually_consistent: 'true' });");
            session.RunAsync("call db.index.fulltext.createNodeIndex('catalogTitles', ['Catalog'], ['title'], { analyzer:'russian', eventually_consistent: 'true' });");
            session.RunAsync("call db.index.fulltext.createNodeIndex('catalogLabels', ['Catalog'], ['labels'], { analyzer: 'russian', eventually_consistent: 'true' });");
            session.RunAsync("call db.index.fulltext.createNodeIndex('labelUniqName', ['Label'], ['uniqName'], { analyzer: 'russian', eventually_consistent: 'true' });");
        }
        //Метод определяет статус запущенной базы данных
        public static async Task<DBState> GetDBState(Neo4j.Driver.IAsyncSession session)
        {
            var result = await session.RunAsync("MATCH (n) RETURN n;");
            List<INode> list = await result.ToListAsync(record => record[0].As<INode>());
            if (list.Count == 0)
                return DBState.Empty;
            else if (GraphDB.GetCatalog(session, "ROOTCAT") == null && list.Count > 0)
                return DBState.HasErrors;
            return DBState.Normal;
        }
    }
}
