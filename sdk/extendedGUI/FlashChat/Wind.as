package
{
   import adobe.utils.*;
   import com.Window;
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
   
   public dynamic class Wind extends Window
   {
      public function Wind()
      {
         super();
         this.__setProp_tf1_Wind_controls_0();
         this.__setProp_sb_Wind_controls_0();
      }
      
      internal function __setProp_tf1_Wind_controls_0() : *
      {
         try
         {
            tf1["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         tf1.autoSize = "none";
         tf1.dropdown = "DefaultScrollingList";
         tf1.enabled = true;
         tf1.enableInitCallback = false;
         tf1.focusable = true;
         tf1.itemRenderer = "ListItemOne";
         tf1.menuDirection = "up";
         tf1.menuMargin = 1;
         tf1.inspectableMenuOffset = {
            "top":0,
            "right":0,
            "bottom":0,
            "left":0
         };
         tf1.inspectableMenuPadding = {
            "top":0,
            "right":0,
            "bottom":0,
            "left":0
         };
         tf1.menuRowCount = 1;
         tf1.menuWidth = -1;
         tf1.menuWrapping = "normal";
         tf1.scrollBar = "";
         tf1.inspectableThumbOffset = {
            "top":0,
            "bottom":0
         };
         tf1.visible = true;
         try
         {
            tf1["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
      
      internal function __setProp_sb_Wind_controls_0() : *
      {
         try
         {
            sb["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         sb.enabled = true;
         sb.enableInitCallback = false;
         sb.minThumbSize = 10;
         sb.offsetBottom = 0;
         sb.offsetTop = 0;
         sb.scrollTarget = "txt";
         sb.trackMode = "scrollToCursor";
         sb.visible = true;
         try
         {
            sb["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
   }
}

