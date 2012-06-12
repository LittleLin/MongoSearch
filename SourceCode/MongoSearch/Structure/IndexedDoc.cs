using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace MongoSearch.Structure
{
    /// <summary>
    /// 建索引原始文件結構
    /// </summary>
    public class SourceDocument
    {
        /// <summary>
        /// 全文欄位
        /// </summary>
        [JsonProperty(PropertyName = "full_text")]
        public Dictionary<String, String> FullText { get; set; }

        /// <summary>
        /// Meta Data 欄位
        /// </summary>
        [JsonProperty(PropertyName = "meta_data")]
        public Dictionary<String, String> MetaData { get; set; }

        public SourceDocument()
        {
            this.FullText = new Dictionary<string, string>();
            this.MetaData = new Dictionary<string, string>();
        }
    }
}
