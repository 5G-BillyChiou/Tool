namespace Tool.ViewModel.Options
{
    public class RepoOption
    {
        /// <summary>
        ///
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// 測試營運商 ( 不進行彙總 )
        /// </summary>
        public string[] TestOperatorIds { get; set; }
    }
}
