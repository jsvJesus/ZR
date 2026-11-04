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
   
   public dynamic class FormHelp extends MovieClip
   {
      public var Contents_label:TextField;
      
      public var Exit_label:TextField;
      
      public var closeBtn:s_closeBtn;
      
      public var content:Content;
      
      public var find:TextField;
      
      public var head:FormHead;
      
      public var list:helpListS;
      
      public var maska:maskList;
      
      public var sbList:DefaultScrollBar;
      
      public var sbText:DefaultScrollBar;
      
      public function FormHelp()
      {
         super();
         this.__setProp_sbList_FormHelp_scrolling_0();
      }
      
      internal function __setProp_sbList_FormHelp_scrolling_0() : *
      {
         try
         {
            this.sbList["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         this.sbList.enabled = true;
         this.sbList.enableInitCallback = false;
         this.sbList.minThumbSize = 10;
         this.sbList.offsetBottom = 0;
         this.sbList.offsetTop = 0;
         this.sbList.scrollTarget = "list";
         this.sbList.trackMode = "scrollPage";
         this.sbList.visible = true;
         try
         {
            this.sbList["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
   }
}

