package FlashHelp_fla
{
   import adobe.utils.*;
   import com.forms.HelpWindow2;
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
   
   public dynamic class MainTimeline extends MovieClip
   {
      public var tt:HelpWindow2;
      
      public function MainTimeline()
      {
         super();
      }
      
      public function onResize(e:Event) : *
      {
         this.tt.x = ((Object(root).width - stage.stageWidth) / 2 + stage.stageWidth) / 2 - this.tt.WIDTH / 2;
         this.tt.y = ((Object(root).height - stage.stageHeight) / 2 + stage.stageHeight) / 2 - this.tt.HEIGHT / 2;
      }
   }
}

