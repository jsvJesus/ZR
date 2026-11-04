package
{
   import adobe.utils.*;
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
   
   public dynamic class DefChanals extends MovieClip
   {
      public var Channels:ChannelList;
      
      public var sb:DefaultScrollBar;
      
      public function DefChanals()
      {
         super();
         this.__setProp_Channels_DefChanals_Слой1_0();
         this.__setProp_sb_DefChanals_Слой1_0();
      }
      
      internal function __setProp_Channels_DefChanals_Слой1_0() : *
      {
         try
         {
            this.Channels["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         this.Channels.enabled = true;
         this.Channels.enableInitCallback = false;
         this.Channels.focusable = true;
         this.Channels.itemRendererName = "ColorChannelCheckBox";
         this.Channels.itemRendererInstanceName = "";
         this.Channels.margin = 0;
         this.Channels.inspectablePadding = {
            "top":0,
            "right":0,
            "bottom":0,
            "left":0
         };
         this.Channels.rowHeight = 0;
         this.Channels.scrollBar = "";
         this.Channels.visible = true;
         this.Channels.wrapping = "normal";
         try
         {
            this.Channels["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
      
      internal function __setProp_sb_DefChanals_Слой1_0() : *
      {
         try
         {
            this.sb["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         this.sb.enabled = true;
         this.sb.enableInitCallback = false;
         this.sb.minThumbSize = 10;
         this.sb.offsetBottom = 0;
         this.sb.offsetTop = 0;
         this.sb.scrollTarget = "Channels";
         this.sb.trackMode = "scrollPage";
         this.sb.visible = true;
         try
         {
            this.sb["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
   }
}

