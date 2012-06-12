using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace MongoSearch.Structure
{
    /// <summary>
    /// 搜尋結果項
    /// </summary>
    public class ResultItem
    {
        /// <summary>
        /// 本結果項的名次
        /// </summary>
        [JsonProperty(PropertyName = "rank")]
        public int Rank { get; set; }

        /// <summary>
        /// 本結果項的分數
        /// </summary>
        [JsonProperty(PropertyName = "score")]
        public int Score { get; set; }

        /// <summary>
        /// 本結果項的範例段落
        /// </summary>
        [JsonProperty(PropertyName = "hit_field")]
        public String HitField { get; set; }
    }

    /// <summary>
    /// 搜尋結果
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// 記錄幾份文件被搜尋到
        /// </summary>
        [JsonProperty(PropertyName = "matches")]
        public int Matches { get; set; }

        /// <summary>
        /// 搜尋所費時間
        /// </summary>
        [JsonProperty(PropertyName = "search_time")]
        public Double SearchTime { get; set; }

        /// <summary>
        /// 搜尋結果項
        /// </summary>
        [JsonProperty(PropertyName = "results")]
        public List<ResultItem> Results { get; set; }

        /// <summary>
        /// 轉為 JSON 格式
        /// </summary>
        /// <returns></returns>
        public String ToJson()
        {
            try
            {
                return JsonConvert.SerializeObject(this);
            }
            catch
            {
                return "";
            }
        }

        public SearchResult()
        {
            Matches = 0;
            Results = new List<ResultItem>();
        }
    }
}
