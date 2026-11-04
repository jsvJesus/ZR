package
{
   import com.dvalimona.components.HtmlTextArea;
   import communication.Api;
   import communication.News;
   import events.ApiEvent;
   import flash.display.Sprite;
   import flash.display.StageAlign;
   import flash.display.StageScaleMode;
   import flash.events.Event;
   import flash.external.ExternalInterface;
   import flash.utils.setTimeout;
   import lang.Locale;
   import logging.Logger;
   import ui.Background;
   
   [SWF(frameRate="60",width="1024",height="768",backgroundColor="0x000000")]
   public class NewsBody extends Sprite
   {
      private var message:HtmlTextArea;
      
      private var bg:Background;
      
      private var drawDebugLayer:Boolean = false;
      
      public function NewsBody()
      {
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.stage.align = StageAlign.TOP_LEFT;
         this.stage.scaleMode = StageScaleMode.NO_SCALE;
         Logger.LogToChannel(Logger.DEBUG,"onAddedToStage");
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         setTimeout(this.initialize,100);
      }
      
      private function initialize() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"initialize");
         Logger.init(this,false);
         Base.stage = this.stage;
         this.stage.addEventListener(Event.RESIZE,this.onStageResize);
         this.prepareFonts();
         this.initBackground();
         this.initCommunication();
         this.initLocales(this.initTextArea);
         Api.self.addEventListener(Api.SHOW_NEWS,this.onNewsHandler);
         this.invalidate();
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
      
      protected function onStageResize(event:Event) : void
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
         var testText:String = "";
         var linkTest:String = "<font color=\'#ffffff\' size=\'22\' face=\'GUIRegular\'>" + "<p>" + "Hello, this is <font color=\'#54bdff\'><a href=\'event:http://www.ya.ru\'>link</a>.</font>" + "</font>" + "</p>";
         testText = "You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS. You can also create new styles and tags by using CSS.";
         Logger.LogToChannel(Logger.DEBUG,"test",testText);
      }
      
      protected function onNewsHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"onNewsHandler");
         News.content = "<font color=\'#ffffff\' size=\'22\' face=\'GUILight\'>" + event.data.answer.description + "</font>";
         News.title = "<p><font color=\'#ffffff\' size=\'26\' face=\'GUIBold\'>" + event.data.answer.title + "</font></p>";
         News.source = event.data.answer.source;
         News.textLink = "<p>" + "<font color=\'#54bdff\' size=\'22\'><a href=\'event:" + News.source + "\'>" + Locale.getById("NewsWidget.NewsWindow.newsArchiveTextLink") + "</a></font>" + "</font>" + "</p>";
         this.showNews();
      }
      
      protected function showNews() : void
      {
         this.message.text = "<font color=\'#ffffff\' size=\'22\' face=\'GUILight\'>" + News.textLink + "<br/>" + News.title + News.content + "<br/>" + News.textLink + "</font>";
         this.invalidate();
      }
      
      protected function prepareFonts() : void
      {
         Base.Light = new GUILightClass();
         Base.FONT_LIGHT = Base.Light.fontName;
         Logger.LogToChannel(Logger.DEFAULT,"SWC light font:",Base.Light.fontName,Base.Light.fontStyle,Base.Light.fontType);
         Base.Regular = new GUIRegularClass();
         Base.FONT_REGULAR = Base.Regular.fontName;
         Logger.LogToChannel(Logger.DEFAULT,"SWC regular font:",Base.Regular.fontName,Base.Regular.fontStyle,Base.Regular.fontType);
         Base.Bold = new GUIBoldClass();
         Base.FONT_BOLD = Base.Bold.fontName;
         Logger.LogToChannel(Logger.DEFAULT,"SWC bold font:",Base.Bold.fontName,Base.Bold.fontStyle,Base.Bold.fontType);
      }
      
      private function initLocales(onSuccess:Function) : void
      {
         Locale.onSuccess = onSuccess;
         Locale.load(["NewsWidget"],true);
      }
      
      private function initCommunication() : void
      {
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
      
      private function externalInterfaceTransmit(response:String) : void
      {
         Api.onCallBack(response);
      }
      
      protected function invalidate() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"invalidate");
         this.addEventListener(Event.ENTER_FRAME,this.onEnterFrame);
      }
      
      protected function onEnterFrame(event:Event) : void
      {
         this.removeEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.draw();
      }
      
      protected function draw() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"draw");
         if(Boolean(this.message) && Boolean(Base.stage))
         {
            this.message.width = Base.stage.stageWidth;
            this.message.height = Base.stage.stageHeight;
         }
         if(this.drawDebugLayer && Boolean(Base.stage))
         {
            this.graphics.clear();
            this.graphics.beginFill(16777215,0.5);
            this.graphics.drawRect(0,0,Base.stage.stageWidth,Base.stage.stageHeight);
            this.graphics.endFill();
         }
      }
   }
}

