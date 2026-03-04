using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FullMetal
{
    public class EventManager : Singleton<EventManager>
    {
        public delegate void TagTouchedDelegate(string tag, bool m_isLeft, GameObject gameObject);
        public TagTouchedDelegate _tagTouchedEvent;
    }

}