package com.dvalimona.components
{
   import flash.display.*;
   import flash.events.*;
   import flash.net.*;
   import flash.text.Font;
   
   public class FontLoader extends EventDispatcher
   {
      private static var font:Font;
      
      private var url:String;
      
      private var loader:Loader;
      
      public var onSuccess:Function;
      
      public var onError:Function;
      
      public function FontLoader(target:IEventDispatcher = null)
      {
         super(target);
      }
      
      public static function getRegularFontName(... args) : String
      {
         return "Bender";
      }
      
      public static function getLightFontName(... args) : String
      {
         return "Bender Light";
      }
      
      public static function getBoldFontName(... args) : String
      {
         return "Bender Bold";
      }
      
      public static function getBlackFontName(... args) : String
      {
         return "Bender Black";
      }
      
      public function load(url:String, onSuccess:Function = null, onError:Function = null) : void
      {
         var request:URLRequest;
         this.url = url;
         this.onSuccess = onSuccess;
         this.onError = onError;
         this.loader = new Loader();
         this.configureListeners(this.loader.contentLoaderInfo);
         request = new URLRequest(this.url);
         try
         {
            this.loader.load(request);
         }
         catch(error:Error)
         {
            trace("Unable to load requested document.");
            if(this.onError != null)
            {
               this.onError(new ErrorEvent(ErrorEvent.ERROR,false,false,"Error: just can\'t load resource."));
            }
         }
      }
      
      private function configureListeners(dispatcher:IEventDispatcher) : void
      {
         dispatcher.addEventListener(Event.COMPLETE,this.completeHandler);
         dispatcher.addEventListener(Event.OPEN,this.openHandler);
         dispatcher.addEventListener(ProgressEvent.PROGRESS,this.progressHandler);
         dispatcher.addEventListener(SecurityErrorEvent.SECURITY_ERROR,this.securityErrorHandler);
         dispatcher.addEventListener(HTTPStatusEvent.HTTP_STATUS,this.httpStatusHandler);
         dispatcher.addEventListener(IOErrorEvent.IO_ERROR,this.ioErrorHandler);
      }
      
      private function completeHandler(event:Event) : void
      {
         trace("completeHandler");
         var MyFont:Class = this.loader.contentLoaderInfo.applicationDomain.getDefinition("StalkerRegular") as Class;
         trace("MyFont",MyFont);
         var embeddedFont:Font = new MyFont();
         font = new MyFont();
         trace("font",font,font.fontName,font.fontStyle,font.fontType);
         if(this.onSuccess != null)
         {
            this.onSuccess();
         }
      }
      
      private function openHandler(event:Event) : void
      {
         trace("openHandler: " + event);
      }
      
      private function progressHandler(event:ProgressEvent) : void
      {
         trace("progressHandler loaded:" + event.bytesLoaded + " total: " + event.bytesTotal);
      }
      
      private function securityErrorHandler(event:SecurityErrorEvent) : void
      {
         trace("securityErrorHandler: " + event);
         if(this.onError != null)
         {
            this.onError(event);
         }
      }
      
      private function httpStatusHandler(event:HTTPStatusEvent) : void
      {
         trace("httpStatusHandler: " + event);
      }
      
      private function ioErrorHandler(event:IOErrorEvent) : void
      {
         trace("ioErrorHandler: " + event);
         if(this.onError != null)
         {
            this.onError(event);
         }
      }
   }
}

