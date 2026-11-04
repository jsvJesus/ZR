package communication
{
   import events.ApiEvent;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import flash.events.IEventDispatcher;
   import logging.Logger;
   
   public class FileManager extends EventDispatcher
   {
      private static var _self:FileManager;
      
      public static const LIST:String = "file_list";
      
      public static const ROOTS:String = "system_roots";
      
      private static var _list:Array = [];
      
      private static var _roots:Array = [];
      
      private static var _isRoot:Boolean = false;
      
      private static var _absPath:String = "";
      
      public function FileManager(target:IEventDispatcher = null)
      {
         super(target);
      }
      
      public static function get FileList() : Array
      {
         return _list;
      }
      
      public static function get RootList() : Array
      {
         return _roots;
      }
      
      public static function get self() : FileManager
      {
         if(!_self)
         {
            _self = new FileManager();
         }
         return _self;
      }
      
      public static function get isRoot() : Boolean
      {
         return _isRoot;
      }
      
      public static function get absPath() : String
      {
         return _absPath;
      }
      
      public static function Init() : void
      {
         addRootsHandler();
         addFileListHandler();
      }
      
      protected static function addRootsHandler() : void
      {
         Api.self.addEventListener(Api.FILESYSTEM_ROOTS,onFileSystemRootsHandler);
      }
      
      protected static function onFileSystemRootsHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"FileManager",event.data.name,event.data.answer);
         _roots = event.data.answer as Array;
         self.dispatchEvent(new Event(ROOTS));
      }
      
      protected static function addFileListHandler() : void
      {
         Api.self.addEventListener(Api.FILELIST,onFileListHandler);
         Logger.LogToChannel(Logger.DEBUG,"FileManager addFileListHandler ok");
      }
      
      protected static function onFileListHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,event.data.name,event.data.answer);
         var dirs:Array = [];
         var files:Array = [];
         _list = event.data.answer.files as Array;
         for(var i:uint = 0; i < _list.length; i++)
         {
            if(_list[i].is_dir == 1)
            {
               dirs.push(_list[i]);
            }
            else
            {
               files.push(_list[i]);
            }
         }
         _list = dirs.concat(files);
         _isRoot = event.data.answer.is_root == "1";
         _absPath = event.data.answer.abs_path;
         Logger.LogToChannel(Logger.DEBUG,"_list",_list);
         Logger.LogToChannel(Logger.DEBUG,"_isRoot",_isRoot);
         Logger.LogToChannel(Logger.DEBUG,"_absPath",_absPath);
         self.dispatchEvent(new Event(LIST));
      }
   }
}

