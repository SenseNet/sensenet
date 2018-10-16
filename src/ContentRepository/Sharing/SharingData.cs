using System;

namespace SenseNet.ContentRepository.Sharing
{
    //UNDONE: implement SharingData

    /// <summary>
    /// Storage model of content sharing information.
    /// </summary>
    internal class SharingData
    {
        public string Token { get; set; }
        public int Identity { get; set; }
        //UNDONE: convert sharing move to enum
        public string Mode { get; set; } //(public/authorized/private)
        //UNDONE: convert sharing level to enum
        public string Level { get; set; } //(open/edit/...)
        public int CreatorId { get; set; }
        public DateTime ShareDate { get; set; }
    }
}
