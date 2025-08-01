<script setup lang="ts">
import { useActiveEntities, useWorkspace } from '@scalar/api-client/store'
import {
  combineRenameDiffs,
  mutateSecuritySchemeDiff,
} from '@scalar/api-client/views/Request/libs'
import { getServersFromOpenApiDocument } from '@scalar/oas-utils/transforms'
import type { OpenAPIV3_1 } from '@scalar/openapi-types'
import type { ApiClientPlugin } from '@scalar/types/api-client'
import type {
  ApiClientConfiguration,
  ApiReferenceConfiguration,
} from '@scalar/types/api-reference'
import { watchDebounced } from '@vueuse/core'
import microdiff from 'microdiff'
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'

import { useNavState } from '@/hooks/useNavState'
import { useExampleStore } from '@/legacy/stores'

import { useApiClient } from './useApiClient'

const { configuration, dereferencedDocument } = defineProps<{
  // The plugins for @scalar/api-reference and @scalar/api-client are different (as of now, doesn't have to be).
  configuration: Partial<Omit<ApiClientConfiguration, 'plugins'>> &
    Pick<ApiReferenceConfiguration, 'onBeforeRequest'>
  dereferencedDocument: OpenAPIV3_1.Document
}>()

const el = ref<HTMLDivElement | null>(null)

const { client, init } = useApiClient()
const { selectedExampleKey, operationId } = useExampleStore()
const activeEntities = useActiveEntities()
const store = useWorkspace()
const { isIntersectionEnabled } = useNavState()

const OnBeforeRequestPlugin: ApiClientPlugin = () => ({
  name: 'on-before-request',
  hooks: {
    onBeforeRequest: configuration.onBeforeRequest,
  },
})

onMounted(() => {
  if (!el.value) {
    return
  }

  // Initialize the client
  init({
    el: el.value,
    configuration: {
      ...configuration,
      // If the onBeforeRequest hook is configured, we add the plugin to the API client.
      plugins: configuration.onBeforeRequest ? [OnBeforeRequestPlugin] : [],
    },
    store,
  })
})

// Ensure we have a document when doing the initial import
watchDebounced(
  () => dereferencedDocument,
  (newDocument) => {
    if (!newDocument) {
      return
    }

    // If we already have a collection, remove the store
    // TODO: add @redis diffing here... or just upgrade to the new store
    if (activeEntities.activeCollection.value) {
      client.value?.resetStore()
    }

    // [re]Import the store
    store.importSpecFile(undefined, 'default', {
      dereferencedDocument: newDocument,
      shouldLoad: false,
      documentUrl: configuration?.url,
      useCollectionSecurity: true,
      ...configuration,
    })
  },
)

// Update the config (non doucment related) on change
watchDebounced(
  () => configuration,
  (newConfig, oldConfig) => {
    if (!oldConfig || !activeEntities.activeCollection.value) {
      return
    }
    const collection = activeEntities.activeCollection.value

    const diff = microdiff(oldConfig, newConfig)
    const documentSourceHasChanged = diff.some(
      (d) =>
        d.path[0] === 'url' ||
        d.path[0] === 'content' ||
        d.path[1] === 'url' ||
        d.path[1] === 'content',
    )

    // If the document source has changed, we re-create the whole store anyway.
    if (documentSourceHasChanged) {
      // Taken care of above
    }
    // Or we handle the specific diff changes, just auth and servers for now
    else {
      diff.forEach((diff) => {
        // Auth - TODO preferredSecurityScheme
        if (diff.path[0] === 'authentication') {
          mutateSecuritySchemeDiff(diff, activeEntities, store)
        }
      })

      // Servers
      if (newConfig.servers || oldConfig.servers) {
        // Delete all the old servers first
        collection.servers.forEach((serverUid) => {
          store.serverMutators.delete(serverUid, collection.uid)
        })

        // Now we either use the new servers or restore the ones from the spec
        const newServers = getServersFromOpenApiDocument(
          newConfig.servers ?? dereferencedDocument.servers,
          {
            baseServerURL: newConfig.baseServerURL,
          },
        )

        // Add the new ones
        newServers.forEach((server) => {
          store.serverMutators.add(server, collection.uid)
        })

        // Select the last server
        if (newServers.length) {
          store.collectionMutators.edit(
            collection.uid,
            'selectedServerUid',
            newServers[newServers.length - 1].uid,
          )
        }
      }
    }

    // Disable intersection observer in case there's some jumpiness
    isIntersectionEnabled.value = false
    setTimeout(() => {
      isIntersectionEnabled.value = true
    }, 1000)
  },
  { deep: true, debounce: 300 },
)

watch(selectedExampleKey, (newKey) => {
  if (client.value && newKey && operationId.value) {
    client.value.updateExample(newKey, operationId.value)
  }
})

onBeforeUnmount(() => client.value?.app.unmount())
</script>

<template>
  <div ref="el" />
</template>
