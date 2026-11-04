package
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.display.*;
   import flash.events.*;
   import flash.external.*;
   import flash.geom.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   import ui.*;
   
   public class TextWidget extends Sprite
   {
      private var message:HtmlTextArea;
      
      private var bg:Background;
      
      private var drawDebugLayer:Boolean = false;
      
      private var title:String = "";
      
      private var text:String = "";
      
      private var source:String = "";
      
      private var textLink:String = "";
      
      private var viewPort:Rectangle;
      
      public function TextWidget()
      {
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
      }
      
      protected function onAddedToStage(param1:Event) : void
      {
         this.stage.align = StageAlign.TOP_LEFT;
         this.stage.scaleMode = StageScaleMode.NO_SCALE;
         Logger.LogToChannel(Logger.DEBUG,"onAddedToStage");
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         setTimeout(this.initialize,100);
         this.draw();
      }
      
      private function initialize() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"initialize");
         Base.stage = this.stage;
         Logger.init(this,false);
         this.stage.addEventListener(Event.RESIZE,this.onStageResize);
         this.prepareFonts();
         this.initBackground();
         this.initCommunication();
         Api.self.addEventListener(Api.SHOW_TEXT,this.onTextHandler);
         Api.self.addEventListener(Api.SCREEN_SIZE,this.onScreenSize);
         this.initLocales(this.initTextArea);
         this.invalidate();
      }
      
      protected function onScreenSize(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onScreenSize");
         this.viewPort = new Rectangle(param1.data.answer.corner[0],param1.data.answer.corner[1],param1.data.answer.width,param1.data.answer.height);
         Logger.LogToChannel(Logger.DEBUG,"onScreenSize",this.viewPort.x,this.viewPort.y,this.viewPort.width,this.viewPort.height);
      }
      
      private function initBackground() : void
      {
         this.bg = new Background();
         this.bg.alpha = 0.1;
         this.addChild(this.bg);
      }
      
      protected function setReady() : void
      {
         Api.call(Api.READY);
         Logger.LogToChannel(Logger.DEBUG,"setReady ok.");
      }
      
      protected function onStageResize(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onStageResize");
         setTimeout(this.invalidate,0);
      }
      
      private function initTextArea() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"initTextArea..");
         this.message = new HtmlTextArea(this);
         this.message.autoHeight = false;
         this.message.autoHideScrollBar = true;
         Logger.LogToChannel(Logger.DEBUG,"initTextArea ok.");
         this.setReady();
      }
      
      private function test() : void
      {
         var _loc1_:* = "";
         var _loc2_:* = "<font color=\'#ffffff\' size=\'22\' face=\'GUIRegular\'>" + "<p>" + "Hello, this is <font color=\'#54bdff\'><a href=\'event:http://www.ya.ru\'>link</a>.</font>" + "</font>" + "</p>";
         _loc1_ = "You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS.";
         Logger.LogToChannel(Logger.DEBUG,"test",_loc1_);
      }
      
      protected function onTextHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onTextHandler");
         if(param1.data.answer.text == null)
         {
            Logger.LogToChannel(Logger.DEBUG,"no text");
         }
         else
         {
            Logger.LogToChannel(Logger.DEBUG,"text.length",param1.data.answer.text.length);
            this.text = "<font color=\'#ffffff\' size=\'22\' face=\'GUILight\'>" + param1.data.answer.text + "</font>";
         }
         if(param1.data.answer.title == null)
         {
            Logger.LogToChannel(Logger.DEBUG,"no title");
         }
         else
         {
            Logger.LogToChannel(Logger.DEBUG,"title.length",param1.data.answer.title.length);
            this.title = "<p><font color=\'#ffffff\' size=\'26\' face=\'GUIBold\'>" + param1.data.answer.title + "</font></p>";
         }
         if(param1.data.answer.source == null)
         {
            Logger.LogToChannel(Logger.DEBUG,"no source");
         }
         else
         {
            Logger.LogToChannel(Logger.DEBUG,"source.length",param1.data.answer.source.length,param1.data.answer.source);
            this.source = param1.data.answer.source;
            this.textLink = "" + "<p>" + "<font color=\'#54bdff\' size=\'22\'>" + "<a href=\'event:" + param1.data.answer.source + "\'>" + Locale.getById("NewsWidget.NewsWindow.newsArchiveTextLink") + "</a>" + "</font>" + "</p>";
         }
         this.showText();
      }
      
      protected function showText() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"showText");
         Logger.LogToChannel(Logger.DEBUG,"source",this.source);
         Logger.LogToChannel(Logger.DEBUG,"title",this.title);
         Logger.LogToChannel(Logger.DEBUG,"text",this.text);
         this.message.text = "<font color=\'#ffffff\' size=\'22\' face=\'GUILight\'>" + this.textLink + this.title + this.text + this.textLink + "</font>";
         this.invalidate();
      }
      
      protected function prepareFonts() : void
      {
         Light = new GUILight();
         FONT_LIGHT = "GUILight";
         Regular = new GUIRegular();
         FONT_REGULAR = "GUIRegular";
         Bold = new GUIBold();
         FONT_BOLD = "GUIBold";
         Bullets = new JustBulletsClass();
         FONT_BULLETS = "JustBulletsClass";
      }
      
      private function initLocales(param1:Function) : void
      {
         Locale.onSuccess = param1;
         Locale.load(["NewsWidget"],true);
      }
      
      private function initCommunication() : void
      {
         var loc1:* = undefined;
         if(ExternalInterface.available)
         {
            try
            {
               ExternalInterface.addCallback("externalInterfaceTransmit",this.externalInterfaceTransmit);
               Logger.LogToChannel(Logger.DEBUG,"ExternalInterface addCallback ok.");
            }
            catch(error:Error)
            {
               Logger.LogToChannel(Logger.ERROR,"Error while ExternalInterface callback initializing:",error.message);
            }
         }
         else
         {
            Logger.LogToChannel(Logger.ERROR,"External Interface is not available on initCommunication!");
         }
      }
      
      private function externalInterfaceTransmit(param1:Object) : void
      {
         var _loc2_:* = undefined;
         for(_loc2_ in param1)
         {
            trace(_loc2_,":",param1[_loc2_]);
         }
         Api.onCallBack(param1);
      }
      
      protected function invalidate() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"invalidate");
         this.addEventListener(Event.ENTER_FRAME,this.onEnterFrame);
      }
      
      protected function onEnterFrame(param1:Event) : void
      {
         this.removeEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.draw();
      }
      
      protected function draw() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"draw");
         if(Boolean(this.message) && Boolean(Base.stage))
         {
            if(this.viewPort == null)
            {
               this.message.width = Base.stage.stageWidth;
               this.message.height = Base.stage.stageHeight;
            }
            else
            {
               this.message.x = this.viewPort.x;
               this.message.y = this.viewPort.y;
               this.message.width = this.viewPort.width;
               this.message.height = this.viewPort.height;
            }
         }
         if(Boolean(this.drawDebugLayer) && Boolean(Base.stage))
         {
            if(this.viewPort == null)
            {
               this.graphics.clear();
               this.graphics.moveTo(0,0);
               this.graphics.lineStyle(1,16777215);
               this.graphics.lineTo(Base.stage.stageWidth,Base.stage.stageHeight);
               this.graphics.moveTo(0,Base.stage.stageHeight);
               this.graphics.lineTo(Base.stage.stageWidth,0);
               this.graphics.beginFill(16777215,0.5);
               this.graphics.drawRect(0,0,Base.stage.stageWidth,Base.stage.stageHeight);
               this.graphics.endFill();
            }
            else
            {
               this.graphics.clear();
               this.graphics.moveTo(this.viewPort.x,this.viewPort.y);
               this.graphics.lineStyle(1,16777215);
               this.graphics.lineTo(this.viewPort.x + this.viewPort.width,this.viewPort.y + this.viewPort.height);
               this.graphics.moveTo(this.viewPort.x,this.viewPort.y + this.viewPort.height);
               this.graphics.lineTo(this.viewPort.x + this.viewPort.width,this.viewPort.y);
               this.graphics.beginFill(16777215,0.5);
               this.graphics.drawRect(this.viewPort.x,this.viewPort.y,this.viewPort.width,this.viewPort.height);
               this.graphics.endFill();
            }
         }
      }
   }
}

