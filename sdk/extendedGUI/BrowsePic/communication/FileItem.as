package communication
{
   public class FileItem
   {
      public var label:String;
      
      public var isDir:Boolean;
      
      public var isUpLevel:Boolean;
      
      public var isRoot:Boolean;
      
      public function FileItem(initObject:Object)
      {
         super();
         this.label = Boolean(initObject) && initObject.label != null ? String(initObject.label) : "error";
         this.isDir = Boolean(initObject) && initObject.isDir != null ? Boolean(initObject.isDir == 1) : Boolean(null);
         this.isUpLevel = Boolean(initObject) && initObject.isUpLevel != null ? Boolean(initObject.isUpLevel) : Boolean(null);
         this.isRoot = Boolean(initObject) && initObject.isRoot != null ? Boolean(initObject.isRoot) : Boolean(null);
      }
   }
}

