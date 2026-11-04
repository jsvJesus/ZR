package
{
   import adobe.utils.*;
   import com.option.OptWindow;
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
   
   public dynamic class OptWind extends OptWindow
   {
      public function OptWind()
      {
         super();
         this.__setProp_BackChatSlider_OptWind_buttons_0();
         this.__setProp_BackGameSlider_OptWind_buttons_0();
         this.__setProp_FontSlider_OptWind_buttons_0();
      }
      
      internal function __setProp_BackChatSlider_OptWind_buttons_0() : *
      {
         try
         {
            BackChatSlider["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         BackChatSlider.enabled = true;
         BackChatSlider.enableInitCallback = false;
         BackChatSlider.focusable = true;
         BackChatSlider.liveDragging = true;
         BackChatSlider.maximum = 1;
         BackChatSlider.minimum = 0;
         BackChatSlider.offsetLeft = 0;
         BackChatSlider.offsetRight = 0;
         BackChatSlider.snapInterval = 1;
         BackChatSlider.snapping = false;
         BackChatSlider.value = 0;
         BackChatSlider.visible = true;
         try
         {
            BackChatSlider["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
      
      internal function __setProp_BackGameSlider_OptWind_buttons_0() : *
      {
         try
         {
            BackGameSlider["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         BackGameSlider.enabled = true;
         BackGameSlider.enableInitCallback = false;
         BackGameSlider.focusable = true;
         BackGameSlider.liveDragging = true;
         BackGameSlider.maximum = 1;
         BackGameSlider.minimum = 0;
         BackGameSlider.offsetLeft = 0;
         BackGameSlider.offsetRight = 0;
         BackGameSlider.snapInterval = 1;
         BackGameSlider.snapping = false;
         BackGameSlider.value = 0;
         BackGameSlider.visible = true;
         try
         {
            BackGameSlider["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
      
      internal function __setProp_FontSlider_OptWind_buttons_0() : *
      {
         try
         {
            FontSlider["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         FontSlider.enabled = true;
         FontSlider.enableInitCallback = false;
         FontSlider.focusable = true;
         FontSlider.liveDragging = true;
         FontSlider.maximum = 24;
         FontSlider.minimum = 11;
         FontSlider.offsetLeft = 0;
         FontSlider.offsetRight = 0;
         FontSlider.snapInterval = 1;
         FontSlider.snapping = true;
         FontSlider.value = 0;
         FontSlider.visible = true;
         try
         {
            FontSlider["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
   }
}

