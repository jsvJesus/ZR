package
{
   import adobe.utils.*;
   import com.TabsBar;
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
   
   public dynamic class TabBar extends TabsBar
   {
      public function TabBar()
      {
         super();
         this.__setProp_TabBarSec_TabBar_Слой1_0();
         this.__setProp_TabBarFirst_TabBar_Слой1_0();
      }
      
      internal function __setProp_TabBarSec_TabBar_Слой1_0() : *
      {
         try
         {
            TabBarSec["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         TabBarSec.columnWidth = 0;
         TabBarSec.direction = "horizontal";
         TabBarSec.enabled = true;
         TabBarSec.enableInitCallback = false;
         TabBarSec.externalColumnCount = 0;
         TabBarSec.focusable = true;
         TabBarSec.itemRendererName = "ButtonTabList";
         TabBarSec.itemRendererInstanceName = "";
         TabBarSec.margin = 0;
         TabBarSec.inspectablePadding = {
            "top":0,
            "right":0,
            "bottom":0,
            "left":0
         };
         TabBarSec.rowHeight = 0;
         TabBarSec.scrollBar = "";
         TabBarSec.visible = true;
         TabBarSec.wrapping = "normal";
         try
         {
            TabBarSec["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
      
      internal function __setProp_TabBarFirst_TabBar_Слой1_0() : *
      {
         try
         {
            TabBarFirst["componentInspectorSetting"] = true;
         }
         catch(e:Error)
         {
         }
         TabBarFirst.columnWidth = 0;
         TabBarFirst.direction = "horizontal";
         TabBarFirst.enabled = true;
         TabBarFirst.enableInitCallback = false;
         TabBarFirst.externalColumnCount = 0;
         TabBarFirst.focusable = true;
         TabBarFirst.itemRendererName = "ButtonTabList";
         TabBarFirst.itemRendererInstanceName = "";
         TabBarFirst.margin = 0;
         TabBarFirst.inspectablePadding = {
            "top":0,
            "right":0,
            "bottom":0,
            "left":0
         };
         TabBarFirst.rowHeight = 0;
         TabBarFirst.scrollBar = "";
         TabBarFirst.visible = true;
         TabBarFirst.wrapping = "normal";
         try
         {
            TabBarFirst["componentInspectorSetting"] = false;
         }
         catch(e:Error)
         {
         }
      }
   }
}

