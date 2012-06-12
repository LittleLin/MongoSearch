MongoSearch
===========

A full-text search engine using MongoDB as backend storage.

Example Code
------------

```csharp
// 1. Create index repository and make index.
Indexer indexer = Indexer.Instance;
Repository repo = indexer.CreateRepository("Test");
repo.AddDocument("{'full_text': {'title': '一級淹水警戒區域 增至9縣市', " +
                 " 'text':'（中央社記者黃巧雯台北12日電）受鋒面和西南氣流影響，各地降豪雨。水利署表示，截至今天下午5時，共18座水庫正洩洪、溢流或調節性放水；一級淹水警戒區域也從11日發布時的4個縣市，增至9個縣市。'}, " +
                 " 'meta_data': {'created_time': '2012/06/01 13:24:33'}}");
repo.MakeIndex();

// 2. Search, returned by JSON format
String jsonResult = repo.Search("淹水", 0, 3).ToJson();
```