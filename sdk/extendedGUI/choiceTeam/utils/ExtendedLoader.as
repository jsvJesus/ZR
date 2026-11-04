package utils
{
   import flash.events.*;
   import flash.net.*;
   
   public class ExtendedLoader extends EventDispatcher
   {
      private var url:String;
      
      private var loader:URLLoader;
      
      public var onSuccess:Function;
      
      public var onError:Function;
      
      public function ExtendedLoader(target:IEventDispatcher = null)
      {
         super(target);
      }
      
      public function load(url:String, onSuccess:Function = null, onError:Function = null) : void
      {
         var request:URLRequest;
         this.url = url;
         this.onSuccess = onSuccess;
         this.onError = onError;
         this.loader = new URLLoader();
         this.loader.dataFormat = StalkerBase.isScaleform ? URLLoaderDataFormat.VARIABLES : URLLoaderDataFormat.TEXT;
         this.configureListeners(this.loader);
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
         if(this.onSuccess != null)
         {
            this.onSuccess(this.loader.data);
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

