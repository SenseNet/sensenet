using System;

namespace SenseNet.ContentRepository.Sharing
{
    //UNDONE: implement SharingData

    /// <summary>
    /// Storage model of sharing information.
    /// </summary>
    internal class SharingData
    {
        public string Token { get; internal set; }
        public int Identity { get; internal set; }
        //UNDONE: convert sharing move to enum
        public string Mode { get; internal set; } //(public/authorized/private)
        //UNDONE: convert sharing level to enum
        public string Level { get; internal set; } //(open/edit/...)
        public int CreatorId { get; internal set; }
        public DateTime ShareDate { get; internal set; }
    }
}
