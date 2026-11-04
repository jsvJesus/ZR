package
{
   import adobe.utils.*;
   import com.forms.EmotionWindow;
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
   
   public dynamic class WindowGUI extends EmotionWindow
   {
      public function WindowGUI()
      {
         super();
         this.__setProp_list_WindowGUI_controls_0();
         this.__setProp_current_list_WindowGUI_controls_0();
         this.__setProp_sb_WindowGUI_buttons_0();
         this.__setProp_sb_current_WindowGUI_buttons_0();
      }
      
      internal function __setProp_list_WindowGUI_controls_0() : *
      {
         try
         {
            list["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         list.columnWidth = 0;
         list.direction = "horizontal";
         list.enabled = true;
         list.enableInitCallback = false;
         list.externalColumnCount = 0;
         list.focusable = true;
         list.itemRendererName = "CheckEmotionBox";
         list.itemRendererInstanceName = "";
         list.margin = 0;
         list.inspectablePadding = {
            "top":0,
            "right":0,
            "bottom":0,
            "left":0
         };
         list.rowHeight = 24;
         list.scrollBar = "sb";
         list.visible = true;
         list.wrapping = "normal";
         try
         {
            list["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
      
      internal function __setProp_current_list_WindowGUI_controls_0() : *
      {
         try
         {
            current_list["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         current_list.columnWidth = 0;
         current_list.direction = "horizontal";
         current_list.enabled = true;
         current_list.enableInitCallback = false;
         current_list.externalColumnCount = 0;
         current_list.focusable = true;
         current_list.itemRendererName = "EmotionButton";
         current_list.itemRendererInstanceName = "";
         current_list.margin = 0;
         current_list.inspectablePadding = {
            "top":0,
            "right":0,
            "bottom":0,
            "left":0
         };
         current_list.rowHeight = 24;
         current_list.scrollBar = "sb";
         current_list.visible = true;
         current_list.wrapping = "normal";
         try
         {
            current_list["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
      
      internal function __setProp_sb_WindowGUI_buttons_0() : *
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
         sb.scrollTarget = "list";
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
      
      internal function __setProp_sb_current_WindowGUI_buttons_0() : *
      {
         try
         {
            sb_current["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         sb_current.enabled = true;
         sb_current.enableInitCallback = false;
         sb_current.minThumbSize = 10;
         sb_current.offsetBottom = 0;
         sb_current.offsetTop = 0;
         sb_current.scrollTarget = "current_list";
         sb_current.trackMode = "scrollToCursor";
         sb_current.visible = true;
         try
         {
            sb_current["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
   }
}

