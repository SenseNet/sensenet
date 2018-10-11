using System;

namespace SenseNet.ContentRepository.Sharing
{
    //UNDONE: implement SharingData

    /// <summary>
    /// Storage model of sharing information.
    /// </summary>
    public class SharingData
    {
        public string Token { get; set; }
        public int Identity { get; set; }
        public string Mode { get; set; } //(public/authorized/private)
        //UNDONE: convert sharing level to enum
        public string Level { get; set; } //(open/edit/...)
        //UNDONE: maybe sharer should be a User
        public int SharerId { get; set; }
        public DateTime ShareDate { get; set; }
    }
}
