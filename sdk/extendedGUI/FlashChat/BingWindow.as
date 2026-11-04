package
{
   import adobe.utils.*;
   import com.translate;
   import flash.accessibility.*;
   import flash.desktop.*;
   import flash.display.*;
   import flash.errors.*;
   import flash.events.*;
   import flash.external.*;
   import flash.filters.*;
   import flash.geom.*;
   import flash.globalization.*;
   import flash.media.*;
   import flash.net.*;
   import flash.net.drm.*;
   import flash.printing.*;
   import flash.profiler.*;
   import flash.sampler.*;
   import flash.sensors.*;
   import flash.system.*;
   import flash.text.*;
   import flash.text.engine.*;
   import flash.text.ime.*;
   import flash.ui.*;
   import flash.utils.*;
   import flash.xml.*;
   
   public dynamic class BingWindow extends translate
   {
      public function BingWindow()
      {
         super();
         this.__setProp_Scroll_BingWindow_backgrouid_0();
         this.__setProp_Scroll2_BingWindow_backgrouid_0();
      }
      
      internal function __setProp_Scroll_BingWindow_backgrouid_0() : *
      {
         try
         {
            Scroll["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         Scroll.enabled = true;
         Scroll.enableInitCallback = false;
         Scroll.minThumbSize = 10;
         Scroll.offsetBottom = 0;
         Scroll.offsetTop = 0;
         Scroll.scrollTarget = "InputText";
         Scroll.trackMode = "scrollToCursor";
         Scroll.visible = true;
         try
         {
            Scroll["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
      
      internal function __setProp_Scroll2_BingWindow_backgrouid_0() : *
      {
         try
         {
            Scroll2["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         Scroll2.enabled = true;
         Scroll2.enableInitCallback = false;
         Scroll2.minThumbSize = 10;
         Scroll2.offsetBottom = 0;
         Scroll2.offsetTop = 0;
         Scroll2.scrollTarget = "OutputText";
         Scroll2.trackMode = "scrollToCursor";
         Scroll2.visible = true;
         try
         {
            Scroll2["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
   }
}

