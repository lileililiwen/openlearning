## 1. Module Setup

- [x] 1.1 Add `Image` and `Document` to `FilePurpose` + migration `AddResourceCenter` (`StoredFile.IsShared` column)
- [x] 1.2 Create `src/OpenLearning.ResourceCenter` class library, add to `OpenLearning.sln`, reference `OpenLearning.Auth`, `OpenLearning.Storage` (never `OpenLearning.Data`)
- [x] 1.3 Register `AddResourceCenterModule` in `Program.cs`; add module to architecture tests
- [x] 1.4 Confirm `dotnet build OpenLearning.sln` — 0 warnings / 0 errors

## 2. Service Layer

- [x] 2.1 `ResourceService.ListAsync(userId, isAdmin, purpose?, search?, page)` — own files + shared + admin-all, ordered newest first, paginated (24/page)
- [x] 2.2 `ResourceService.UploadAsync(userId, purpose, file)` — delegates to `StorageService.UploadAsync` (Image/Document/Video)
- [x] 2.3 `ResourceService.DeleteAsync(key, userId, isAdmin)` — owner/admin only, delegates to `StorageService.DeleteAsync`
- [x] 2.4 `ResourceService.SetSharedAsync(key, userId, isAdmin)` — admin only; rejects private purposes
- [x] 2.5 `ResourceService.GetByIdAsync`/`GetByKeyAsync` for picker rendering

## 3. Pages

- [x] 3.1 `Pages/Resources/Index.cshtml(.cs)` — grid/list, type filter, search, pagination, preview (image/video/doc), copy-URL, delete, admin share toggle
- [x] 3.2 Upload is a form on the Index page (purpose selector + file) returning a copyable URL (no separate Upload page needed)
- [x] 3.3 `Pages/Resources/Picker.cshtml(.cs)` — server-rendered picker (modal iframe): purpose filter + search + grid of selectable cards (24/page), fills the caller's input on selection
- [x] 3.4 Menu entry "资源中心" for Instructor + Admin groups

## 4. Picker Integration

- [x] 4.1 Razor partial `_ResourcePicker` + JS that opens `/Resources/Picker` in a modal, fills the target input, and closes
- [x] 4.2 Wire into lesson Create/Edit `VideoUrl`, `VideoPosterUrl`, `SubtitleUrl` (filter: video / image / document)
- [x] 4.3 Wire into profile avatar `AvatarUrl` (filter: image)
- [x] 4.4 Wire into admin banner `ImageUrl` (filter: image)

## 5. Sharing & ACL

- [x] 5.1 `StoredFile.IsShared` read for listing visibility (own + shared + admin-all)
- [x] 5.2 `SetSharedAsync` rejects `IsPrivate` purposes
- [x] 5.3 Confirm `/files/{key}` serving is unchanged (shared resources are never private)

## 6. Build & Verify

- [x] 6.1 `dotnet build OpenLearning.sln` — 0 warnings / 0 errors
- [x] 6.2 HTTP smoke tests:
  - Instructor uploads image + video + document → 3 resources listed, URLs copyable
  - Filter by Video and search by name → correct subset
  - Invalid extension rejected
  - Owner deletes own resource → gone; non-owner delete denied
  - Admin shares a resource → appears in another user's library
  - Picker on lesson edit fills `VideoUrl`; poster picker only shows images
  - Avatar + banner pickers fill the field
  - Student can open a shared resource URL; cannot list non-shared others
